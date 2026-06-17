using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PlantDoctor.Models;
using System.Diagnostics;

namespace PlantDoctor.Services
{
    public class OnnxInferenceService : IInferenceService
    {
        private InferenceSession? _session;
        private bool _isInitialized = false;

        // Exact alphabetical order of PlantVillage folders
        private static readonly string[] Labels = new[]
        {
            "Pepper__bell___Bacterial_spot",       // 0
            "Pepper__bell___healthy",              // 1
            "Potato___Early_blight",               // 2
            "Potato___Late_blight",                // 3
            "Potato___healthy",                    // 4
            "Tomato_Bacterial_spot",               // 5
            "Tomato_Early_blight",                 // 6
            "Tomato_Late_blight",                  // 7
            "Tomato_Leaf_Mold",                    // 8
            "Tomato_Septoria_leaf_spot",           // 9
            "Tomato_Spider_mites_Two_spotted_spider_mite", // 10
            "Tomato_Target_Spot",                  // 11
            "Tomato_Yellow_Leaf_Curl_Virus",       // 12
            "Tomato_healthy",                      // 13
            "Tomato_mosaic_virus"                  // 14
        };

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // Copy ONNX model from MauiAsset to writable storage on first run
            var modelPath = Path.Combine(FileSystem.AppDataDirectory, "plant_disease_model.onnx");

            if (!File.Exists(modelPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("plant_disease_model.onnx");
                using var fileStream = File.Create(modelPath);
                await stream.CopyToAsync(fileStream);
            }

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(modelPath, options);
            _isInitialized = true;
        }

        public async Task<PredictionResult> PredictAsync(string imagePath)
        {
            await InitializeAsync();

            var stopwatch = Stopwatch.StartNew();

            // Step 1 — Load and resize image to 224x224
            var inputTensor = await PreprocessImageAsync(imagePath);

            // Step 2 — Run inference
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(
                    _session!.InputMetadata.Keys.First(),
                    inputTensor)
            };

            float[] outputScores;
            using (var results = _session.Run(inputs))
            {
                outputScores = results.First().AsEnumerable<float>().ToArray();
            }

            // Step 3 — Apply softmax (model uses from_logits=True)
            var probabilities = Softmax(outputScores);

            // Step 4 — Get top prediction
            var maxIndex = probabilities
                .Select((p, i) => (p, i))
                .OrderByDescending(x => x.p)
                .First().i;

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
            // Load image bytes
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

            // Decode and resize to 224x224 using MAUI's built-in image handling
            var tensor = new DenseTensor<float>(new[] { 1, 224, 224, 3 }); // NHWC format

            await Task.Run(() =>
            {
                // Decode image manually using raw pixel data
                using var ms = new MemoryStream(imageBytes);
                var pixels = DecodeAndResizeImage(ms, 224, 224);

                // Fill tensor — values 0-255 (model has internal Rescaling layer)
                for (int y = 0; y < 224; y++)
                {
                    for (int x = 0; x < 224; x++)
                    {
                        int pixelIndex = (y * 224 + x) * 3;
                        tensor[0, y, x, 0] = pixels[pixelIndex];     // R
                        tensor[0, y, x, 1] = pixels[pixelIndex + 1]; // G
                        tensor[0, y, x, 2] = pixels[pixelIndex + 2]; // B
                    }
                }
            });

            return tensor;
        }

        private float[] DecodeAndResizeImage(Stream imageStream, int width, int height)
        {
            // Read all bytes
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                imageStream.CopyTo(ms);
                imageBytes = ms.ToArray();
            }

            // Use SkiaSharp for reliable cross-platform image decoding
            using var skBitmap = SkiaSharp.SKBitmap.Decode(imageBytes);
            using var resized = skBitmap.Resize(
                new SkiaSharp.SKImageInfo(width, height),
                SkiaSharp.SKFilterQuality.High);

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