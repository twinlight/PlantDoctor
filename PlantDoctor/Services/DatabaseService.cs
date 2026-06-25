using PlantDoctor.Models;
using SQLite;

namespace PlantDoctor.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _db;

        // Bump this version number any time seed data changes
        // App checks this and re-seeds if version doesn't match
        private const int CurrentDbVersion = 3;

        private async Task InitAsync()
        {
            if (_db != null) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "plantdoctor.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            await _db.CreateTableAsync<DiseaseInfo>();
            await _db.CreateTableAsync<ScanHistory>();
            await _db.CreateTableAsync<DbMeta>();

            // Check version — re-seed if outdated or missing
            var meta = await _db.Table<DbMeta>().FirstOrDefaultAsync();
            if (meta == null || meta.Version < CurrentDbVersion)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DB] Version mismatch (found={meta?.Version}, required={CurrentDbVersion}). Re-seeding...");
#endif
                await _db.DeleteAllAsync<DiseaseInfo>();
                await SeedDiseaseDataAsync();

                if (meta == null)
                    await _db.InsertAsync(new DbMeta { Id = 1, Version = CurrentDbVersion });
                else
                {
                    meta.Version = CurrentDbVersion;
                    await _db.UpdateAsync(meta);
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[DB] Re-seed complete.");
#endif
            }
        }

        // ── Scan History ──────────────────────────────────────────────────────
        public async Task SaveScanAsync(ScanHistory scan)
        {
            await InitAsync();
            await _db!.InsertAsync(scan);
        }

        public async Task<List<ScanHistory>> GetHistoryAsync()
        {
            await InitAsync();
            return await _db!.Table<ScanHistory>()
                .OrderByDescending(h => h.ScannedAt)
                .ToListAsync();
        }

        public async Task DeleteScanAsync(int id)
        {
            await InitAsync();
            await _db!.DeleteAsync<ScanHistory>(id);
        }

        public async Task ClearHistoryAsync()
        {
            await InitAsync();
            await _db!.DeleteAllAsync<ScanHistory>();
        }

        // ── Disease Info ──────────────────────────────────────────────────────
        public async Task<DiseaseInfo?> GetDiseaseInfoAsync(int classIndex)
        {
            await InitAsync();
            var result = await _db!.Table<DiseaseInfo>()
                .Where(d => d.ClassIndex == classIndex)
                .FirstOrDefaultAsync();

#if DEBUG
            if (result == null)
                System.Diagnostics.Debug.WriteLine($"[DB] WARNING: No DiseaseInfo found for ClassIndex={classIndex}");
            else
                System.Diagnostics.Debug.WriteLine($"[DB] Found: ClassIndex={classIndex} → {result.DisplayName} ({result.CropName})");
#endif
            return result;
        }

        // ── Seed Data ─────────────────────────────────────────────────────────
        // ClassIndex values MUST match class_labels.json from trained model:
        // 0=Pepper__bell___Bacterial_spot, 1=Pepper__bell___healthy,
        // 2=Potato___Early_blight, 3=Potato___Late_blight, 4=Potato___healthy,
        // 5=Tomato_Bacterial_spot, 6=Tomato_Early_blight, 7=Tomato_Late_blight,
        // 8=Tomato_Leaf_Mold, 9=Tomato_Septoria_leaf_spot,
        // 10=Tomato_Spider_mites_Two_spotted_spider_mite,
        // 11=Tomato__Target_Spot, 12=Tomato__Tomato_YellowLeaf__Curl_Virus,
        // 13=Tomato__Tomato_mosaic_virus, 14=Tomato_healthy
        private async Task SeedDiseaseDataAsync()
        {
            var diseases = new List<DiseaseInfo>
            {
                new DiseaseInfo
                {
                    ClassIndex = 0,
                    ClassName = "Pepper__bell___Bacterial_spot",
                    CropName = "Pepper Bell",
                    DisplayName = "Bacterial Spot",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Bacterial spot is caused by Xanthomonas bacteria and is one of the most damaging diseases of pepper in warm, moist environments.",
                    Symptoms = "Small, water-soaked spots on leaves that turn brown with yellow halos. Spots may merge causing large necrotic areas. Fruit develops raised, scab-like spots.",
                    ChemicalTreatment = "Apply copper-based bactericides (copper hydroxide or copper oxychloride) at 7–10 day intervals. Use Mancozeb combined with copper for better control. Streptomycin sprays can be effective in early stages.",
                    OrganicTreatment = "Spray with copper soap solution weekly. Use Bacillus subtilis-based biocontrol agents. Remove and destroy infected plant debris immediately.",
                    PreventiveMeasures = "Use certified disease-free seeds. Avoid overhead irrigation. Rotate crops for at least 2 years. Maintain plant spacing for airflow. Disinfect tools regularly."
                },
                new DiseaseInfo
                {
                    ClassIndex = 1,
                    ClassName = "Pepper__bell___healthy",
                    CropName = "Pepper Bell",
                    DisplayName = "Healthy",
                    Severity = "Healthy",
                    ColorHex = "#43A047",
                    Description = "The pepper plant appears healthy with no signs of disease or pest infestation.",
                    Symptoms = "Leaves are vibrant green, firm, and free from spots, lesions, or discoloration.",
                    ChemicalTreatment = "No treatment required.",
                    OrganicTreatment = "Continue regular organic fertilization and compost application to maintain soil health.",
                    PreventiveMeasures = "Maintain proper watering schedule. Ensure adequate sunlight and air circulation. Monitor regularly for early signs of disease."
                },
                new DiseaseInfo
                {
                    ClassIndex = 2,
                    ClassName = "Potato___Early_blight",
                    CropName = "Potato",
                    DisplayName = "Early Blight",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Early blight is caused by the fungus Alternaria solani and typically affects older leaves first, especially during warm and humid conditions.",
                    Symptoms = "Dark brown to black lesions with concentric rings forming a target-board pattern. Yellow halo surrounds the lesions. Starts on lower/older leaves and moves upward.",
                    ChemicalTreatment = "Apply fungicides containing Chlorothalonil, Mancozeb, or Azoxystrobin every 7–14 days. Start applications before disease onset or at first sign of symptoms.",
                    OrganicTreatment = "Apply neem oil spray weekly. Use copper-based fungicides. Remove infected leaves promptly. Apply compost tea as a foliar spray to boost plant immunity.",
                    PreventiveMeasures = "Plant certified disease-free seed potatoes. Ensure adequate plant spacing. Avoid overhead watering. Rotate crops with non-solanaceous plants for 2–3 years."
                },
                new DiseaseInfo
                {
                    ClassIndex = 3,
                    ClassName = "Potato___Late_blight",
                    CropName = "Potato",
                    DisplayName = "Late Blight",
                    Severity = "Severe",
                    ColorHex = "#E53935",
                    Description = "Late blight caused by Phytophthora infestans is one of the most destructive potato diseases, responsible for the Irish Potato Famine. It spreads rapidly in cool, wet conditions.",
                    Symptoms = "Water-soaked, pale green to dark brown lesions on leaves with white mold visible on undersides in humid conditions. Brown rot spreads to stems and tubers quickly.",
                    ChemicalTreatment = "Apply systemic fungicides such as Metalaxyl, Dimethomorph, or Cymoxanil combined with contact fungicides. Apply every 5–7 days during high-risk weather. Do not wait for visible symptoms.",
                    OrganicTreatment = "Copper hydroxide sprays applied preventively. Remove and destroy all infected plant material. Avoid composting infected debris. Use resistant varieties where possible.",
                    PreventiveMeasures = "Use certified blight-resistant seed varieties. Hill soil around plants to protect tubers. Destroy volunteer potato plants. Monitor weather forecasts for blight-favorable conditions."
                },
                new DiseaseInfo
                {
                    ClassIndex = 4,
                    ClassName = "Potato___healthy",
                    CropName = "Potato",
                    DisplayName = "Healthy",
                    Severity = "Healthy",
                    ColorHex = "#43A047",
                    Description = "The potato plant appears healthy with no signs of disease or stress.",
                    Symptoms = "Leaves are dark green, firm, and show no spots, lesions, or wilting.",
                    ChemicalTreatment = "No treatment required.",
                    OrganicTreatment = "Maintain soil health with regular composting and balanced fertilization.",
                    PreventiveMeasures = "Hill soil around plants regularly. Water at the base to keep foliage dry. Scout fields weekly for early disease signs."
                },
                new DiseaseInfo
                {
                    ClassIndex = 5,
                    ClassName = "Tomato_Bacterial_spot",
                    CropName = "Tomato",
                    DisplayName = "Bacterial Spot",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Caused by Xanthomonas species, bacterial spot thrives in warm, wet conditions and spreads rapidly through rain splash and wind.",
                    Symptoms = "Small, dark, water-soaked spots on leaves, stems, and fruit. Spots turn brown with yellow margins. Severely infected leaves turn yellow and drop. Fruit shows raised scabby spots.",
                    ChemicalTreatment = "Apply copper-based bactericides every 7 days. Tank mix copper hydroxide with Mancozeb for improved efficacy. Avoid applying in hot midday temperatures.",
                    OrganicTreatment = "Use copper soap sprays weekly. Apply Bacillus subtilis biofungicides. Prune affected leaves and destroy. Avoid working with plants when wet.",
                    PreventiveMeasures = "Use disease-free transplants. Avoid overhead irrigation. Space plants to improve air circulation. Practice 2-year crop rotation. Sanitize tools between plants."
                },
                new DiseaseInfo
                {
                    ClassIndex = 6,
                    ClassName = "Tomato_Early_blight",
                    CropName = "Tomato",
                    DisplayName = "Early Blight",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Early blight caused by Alternaria solani is a common fungal disease that typically starts on older, lower leaves and moves upward under warm and humid conditions.",
                    Symptoms = "Circular dark brown spots with concentric target-like rings. Yellow tissue surrounds spots. Infected leaves eventually yellow and drop. Collar rot may occur at stem base.",
                    ChemicalTreatment = "Apply Chlorothalonil, Mancozeb, or Azoxystrobin-based fungicides every 7–10 days. Start applications preventively in high-risk seasons.",
                    OrganicTreatment = "Weekly neem oil applications. Copper-based organic fungicides. Remove lower infected leaves. Mulch around plant base to prevent soil splash.",
                    PreventiveMeasures = "Stake plants for airflow. Water at the base in the morning. Maintain adequate potassium nutrition. Rotate tomato crops annually."
                },
                new DiseaseInfo
                {
                    ClassIndex = 7,
                    ClassName = "Tomato_Late_blight",
                    CropName = "Tomato",
                    DisplayName = "Late Blight",
                    Severity = "Severe",
                    ColorHex = "#E53935",
                    Description = "Late blight caused by Phytophthora infestans spreads extremely rapidly in cool, moist weather and can destroy entire crops within days if untreated.",
                    Symptoms = "Greasy, dark brown lesions on leaves and stems. White fuzzy sporulation on leaf undersides in humid weather. Fruit develops firm, dark, greasy-looking rot.",
                    ChemicalTreatment = "Apply Metalaxyl + Mancozeb, Cymoxanil, or Famoxadone fungicides every 5–7 days preventively. Increase frequency during wet weather. Rotate fungicide classes to prevent resistance.",
                    OrganicTreatment = "Preventive copper hydroxide sprays. Remove and bag infected plant parts immediately. Do not compost infected material. Destroy entire plant if severely infected.",
                    PreventiveMeasures = "Plant resistant varieties. Avoid overhead irrigation. Ensure good drainage. Monitor weather forecasts. Space plants widely for maximum air circulation."
                },
                new DiseaseInfo
                {
                    ClassIndex = 8,
                    ClassName = "Tomato_Leaf_Mold",
                    CropName = "Tomato",
                    DisplayName = "Leaf Mold",
                    Severity = "Mild",
                    ColorHex = "#FF8F00",
                    Description = "Leaf mold caused by Passalora fulva is most common in greenhouse tomatoes or in humid outdoor conditions with poor air circulation.",
                    Symptoms = "Pale green to yellow spots on upper leaf surface. Olive-green to brown velvety mold on corresponding lower leaf surface. Infected leaves may curl and drop.",
                    ChemicalTreatment = "Apply Chlorothalonil, Mancozeb, or copper-based fungicides every 7 days. Ensure thorough coverage of leaf undersides where spores form.",
                    OrganicTreatment = "Improve ventilation immediately. Apply potassium bicarbonate spray. Use neem oil weekly. Remove heavily infected leaves.",
                    PreventiveMeasures = "Reduce relative humidity below 85%. Increase plant spacing. Prune lower leaves for airflow. Avoid wetting foliage during irrigation."
                },
                new DiseaseInfo
                {
                    ClassIndex = 9,
                    ClassName = "Tomato_Septoria_leaf_spot",
                    CropName = "Tomato",
                    DisplayName = "Septoria Leaf Spot",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Septoria leaf spot is caused by the fungus Septoria lycopersici and is one of the most common tomato diseases, spreading rapidly during wet weather.",
                    Symptoms = "Numerous small circular spots with dark brown borders and gray-white centers. Tiny dark fungal bodies visible in spot centers. Lower leaves affected first, progressing upward.",
                    ChemicalTreatment = "Apply Chlorothalonil, Mancozeb, or Copper hydroxide every 7–10 days. Begin applications when disease first appears or during prolonged wet weather.",
                    OrganicTreatment = "Remove infected leaves immediately. Copper soap sprays weekly. Apply mulch to prevent soil splash. Neem oil as supplemental treatment.",
                    PreventiveMeasures = "Avoid overhead watering. Stake plants off the ground. Remove plant debris at end of season. Rotate crops for at least 2 years."
                },
                new DiseaseInfo
                {
                    ClassIndex = 10,
                    ClassName = "Tomato_Spider_mites_Two_spotted_spider_mite",
                    CropName = "Tomato",
                    DisplayName = "Spider Mites",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Two-spotted spider mites (Tetranychus urticae) are tiny arachnids that feed on plant cells and thrive in hot, dry conditions, rapidly colonizing entire plants.",
                    Symptoms = "Fine stippling or bronzing on leaf upper surface. Webbing visible on leaf undersides and between leaves. Leaves turn yellow, dry, and drop. Tiny moving dots visible under magnification.",
                    ChemicalTreatment = "Apply miticides containing Abamectin, Bifenazate, or Spiromesifen. Rotate miticide classes every 2 applications to prevent resistance. Avoid pyrethroids as they can worsen mite outbreaks.",
                    OrganicTreatment = "Strong water sprays to dislodge mites. Apply neem oil or insecticidal soap every 5–7 days. Introduce predatory mites (Phytoseiulus persimilis) for biological control.",
                    PreventiveMeasures = "Maintain adequate soil moisture and avoid plant stress. Monitor plants in hot weather. Avoid dusty conditions. Avoid broad-spectrum pesticides that kill natural predators."
                },
                new DiseaseInfo
                {
                    ClassIndex = 11,
                    ClassName = "Tomato__Target_Spot",
                    CropName = "Tomato",
                    DisplayName = "Target Spot",
                    Severity = "Moderate",
                    ColorHex = "#FF8F00",
                    Description = "Target spot is caused by the fungus Corynespora cassiicola and affects all above-ground parts of tomato plants in warm, humid tropical and subtropical regions.",
                    Symptoms = "Circular brown lesions with concentric rings resembling a target. Yellow halo may surround spots. Lesions on fruit appear as sunken, dark spots. Defoliation occurs in severe cases.",
                    ChemicalTreatment = "Apply Azoxystrobin, Pyraclostrobin, or Chlorothalonil fungicides every 7–14 days. Ensure good canopy coverage during application.",
                    OrganicTreatment = "Apply copper-based sprays preventively. Remove infected leaves promptly. Apply biological fungicides containing Trichoderma species.",
                    PreventiveMeasures = "Improve air circulation through proper spacing and pruning. Avoid overhead watering. Remove plant debris from previous season. Use mulch to prevent soil splash."
                },
                new DiseaseInfo
                {
                    ClassIndex = 12,
                    ClassName = "Tomato__Tomato_YellowLeaf__Curl_Virus",
                    CropName = "Tomato",
                    DisplayName = "Yellow Leaf Curl Virus",
                    Severity = "Severe",
                    ColorHex = "#E53935",
                    Description = "Tomato Yellow Leaf Curl Virus (TYLCV) is transmitted by whiteflies and is one of the most devastating viral diseases of tomato worldwide with no cure once infected.",
                    Symptoms = "Upward curling and yellowing of leaf margins. Leaves appear small, crumpled, and cup-shaped. Stunted plant growth. Severely reduced fruit set. Whiteflies often visible on plant.",
                    ChemicalTreatment = "No cure for infected plants. Control whitefly vectors using Imidacloprid, Thiamethoxam, or Spirotetramat systemic insecticides. Apply within 2 weeks of transplanting for prevention.",
                    OrganicTreatment = "Use yellow sticky traps to monitor and reduce whitefly populations. Apply neem oil or insecticidal soap to control whiteflies. Use reflective silver mulches to repel whiteflies.",
                    PreventiveMeasures = "Plant resistant or tolerant tomato varieties. Use insect-proof screens in greenhouse production. Remove and destroy infected plants immediately to prevent spread. Avoid planting near infected crops."
                },
                new DiseaseInfo
                {
                    ClassIndex = 13,
                    ClassName = "Tomato__Tomato_mosaic_virus",
                    CropName = "Tomato",
                    DisplayName = "Mosaic Virus",
                    Severity = "Severe",
                    ColorHex = "#E53935",
                    Description = "Tomato Mosaic Virus (ToMV) spreads through contact with infected plant sap, contaminated tools, and hands. It can persist in soil and plant debris for years.",
                    Symptoms = "Mottled light and dark green mosaic pattern on leaves. Leaves may be distorted, curled, or reduced in size. Stunted plant growth. Fruit may show yellow mottling and internal browning.",
                    ChemicalTreatment = "No chemical cure for viral infections. Remove and destroy infected plants immediately. Disinfect tools with 10% bleach or 70% alcohol solution between plants.",
                    OrganicTreatment = "No organic cure available. Prevention is the only management strategy. Remove infected plants and wash hands thoroughly after handling.",
                    PreventiveMeasures = "Use certified virus-free seeds and transplants. Wash hands thoroughly before handling plants. Disinfect tools regularly. Control aphid populations which can spread related viruses. Do not smoke near plants as tobacco can harbor mosaic virus."
                },
                new DiseaseInfo
                {
                    ClassIndex = 14,
                    ClassName = "Tomato_healthy",
                    CropName = "Tomato",
                    DisplayName = "Healthy",
                    Severity = "Healthy",
                    ColorHex = "#43A047",
                    Description = "The tomato plant appears healthy with no visible signs of disease, pest damage, or nutritional deficiency.",
                    Symptoms = "Leaves are deep green, firm, and show no spots, lesions, curling, or discoloration.",
                    ChemicalTreatment = "No treatment required.",
                    OrganicTreatment = "Continue regular balanced fertilization. Apply compost tea as a foliar spray to boost natural immunity.",
                    PreventiveMeasures = "Water at the base in the morning. Stake plants for support. Scout weekly for early pest and disease signs. Maintain good soil drainage."
                }
            };

            await _db!.InsertAllAsync(diseases);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DB] Seeded {diseases.Count} disease records.");
            foreach (var d in diseases)
                System.Diagnostics.Debug.WriteLine($"  [{d.ClassIndex:D2}] {d.ClassName} → {d.DisplayName}");
#endif
        }
    }
}
