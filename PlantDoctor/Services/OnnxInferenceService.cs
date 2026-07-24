using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PlantDoctor.Models;
using System.Diagnostics;

namespace PlantDoctor.Services
{
    public class OnnxInferenceService : IInferenceService, IDisposable
    {
        private readonly object _initLock = new();
        private InferenceSession? _session;
        private bool _isInitialized;

        // Synchronized with class_labels.json from trained model
        // Order = alphabetical folder sort by image_dataset_from_directory
        private static readonly string[] Labels = new[]
        {
            "Pepper__bell___Bacterial_spot",                    // 0
            "Pepper__bell___healthy",                           // 1
            "Potato___Early_blight",                            // 2
            "Potato___Late_blight",                             // 3
            "Potato___healthy",                                 // 4
            "Tomato_Bacterial_spot",                            // 5
            "Tomato_Early_blight",                              // 6
            "Tomato_Late_blight",                               // 7
            "Tomato_Leaf_Mold",                                 // 8
            "Tomato_Septoria_leaf_spot",                        // 9
            "Tomato_Spider_mites_Two_spotted_spider_mite",      // 10
            "Tomato__Target_Spot",                              // 11
            "Tomato__Tomato_YellowLeaf__Curl_Virus",            // 12
            "Tomato__Tomato_mosaic_virus",                      // 13
            "Tomato_healthy"                                    // 14
        };

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            var modelPath = Path.Combine(FileSystem.AppDataDirectory, "plant_disease_model.onnx");
            if (!File.Exists(modelPath))
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync("plant_disease_model.onnx");
                await using var fileStream = File.Create(modelPath);
                await stream.CopyToAsync(fileStream);
            }

            await Task.Run(() =>
            {
                lock (_initLock)
                {
                    if (_isInitialized) return;

                    _session?.Dispose();
                    var options = new SessionOptions
                    {
                        GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                        InterOpNumThreads = 1,
                        IntraOpNumThreads = 1
                    };

                    _session = new InferenceSession(modelPath, options);
                    _isInitialized = true;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ONNX] Model loaded from: {modelPath}");
                    System.Diagnostics.Debug.WriteLine($"[ONNX] Input: {string.Join(", ", _session.InputMetadata.Keys)}");
                    System.Diagnostics.Debug.WriteLine($"[ONNX] Output: {string.Join(", ", _session.OutputMetadata.Keys)}");
#endif
                }
            });
        }

        private void ResetSession()
        {
            lock (_initLock)
            {
                _session?.Dispose();
                _session = null;
                _isInitialized = false;
            }
        }

        public void Dispose()
        {
            ResetSession();
        }

        public async Task<PredictionResult> PredictAsync(string imagePath)
        {
            await InitializeAsync();

            var stopwatch = Stopwatch.StartNew();
            var inputTensor = await PreprocessImageAsync(imagePath);

            float[] outputScores;
            try
            {
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(
                        _session!.InputMetadata.Keys.First(),
                        inputTensor)
                };

                using var results = _session.Run(inputs);
                outputScores = results.First().AsEnumerable<float>().ToArray();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ONNX] Inference failed, resetting session: {ex}");
#endif
                ResetSession();
                throw;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ONNX] Raw output scores ({outputScores.Length} values): [{string.Join(", ", outputScores.Select(s => s.ToString("F3")))}]");
#endif

            // Apply softmax — model uses from_logits=True
            var probabilities = Softmax(outputScores);

            var maxIndex = probabilities
                .Select((p, i) => (p, i))
                .OrderByDescending(x => x.p)
                .First().i;

#if DEBUG
            System.Diagnostics.Debug.WriteLine("=== PREDICTION DEBUG ===");
            for (int i = 0; i < probabilities.Length; i++)
            {
                var marker = i == maxIndex ? " ◄ TOP" : "";
                System.Diagnostics.Debug.WriteLine($"  [{i:D2}] {Labels[i],-52}: {probabilities[i]:P2}{marker}");
            }
            System.Diagnostics.Debug.WriteLine($"=== RESULT: index={maxIndex}, label={Labels[maxIndex]}, confidence={probabilities[maxIndex]:P2} ===");
#endif

            stopwatch.Stop();

            return new PredictionResult
            {
                ClassIndex = maxIndex,
                ClassName = Labels[maxIndex],
                Confidence = probabilities[maxIndex],
                InferenceMode = "Offline",
                InferenceTimeMs = stopwatch.ElapsedMilliseconds
            };
        }

        private async Task<DenseTensor<float>> PreprocessImageAsync(string imagePath)
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            var tensor = new DenseTensor<float>(new[] { 1, 224, 224, 3 });

            await Task.Run(() =>
            {
                using var ms = new MemoryStream(imageBytes);
                var pixels = DecodeAndResizeImage(ms, 224, 224);

                for (int y = 0; y < 224; y++)
                {
                    for (int x = 0; x < 224; x++)
                    {
                        int pixelIndex = (y * 224 + x) * 3;
                        tensor[0, y, x, 0] = pixels[pixelIndex];
                        tensor[0, y, x, 1] = pixels[pixelIndex + 1];
                        tensor[0, y, x, 2] = pixels[pixelIndex + 2];
                    }
                }
            });

            return tensor;
        }

        private float[] DecodeAndResizeImage(Stream imageStream, int width, int height)
        {
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                imageStream.CopyTo(ms);
                imageBytes = ms.ToArray();
            }

            using var skBitmap = SkiaSharp.SKBitmap.Decode(imageBytes);
            if (skBitmap == null)
                throw new InvalidOperationException("Could not decode the selected image.");

            using var resized = skBitmap.Resize(
                new SkiaSharp.SKImageInfo(width, height),
                SkiaSharp.SKSamplingOptions.Default);

            float[] pixels = new float[width * height * 3];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = resized.GetPixel(x, y);
                    pixels[index++] = color.Red;
                    pixels[index++] = color.Green;
                    pixels[index++] = color.Blue;
                }
            }

            return pixels;
        }

        private static float[] Softmax(float[] logits)
        {
            float maxLogit = logits.Max();
            float[] exps = logits.Select(l => MathF.Exp(l - maxLogit)).ToArray();
            float sum = exps.Sum();
            return exps.Select(e => e / sum).ToArray();
        }
    }
}