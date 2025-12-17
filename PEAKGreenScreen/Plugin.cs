using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PEAKGreenScreen
{
    [BepInDependency("tony4twentys.Airport_Remixed", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.atomic.greenscreen", "PEAK Green Screen", "1.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        public static ConfigEntry<float> configColorR;
        public static ConfigEntry<float> configColorG;
        public static ConfigEntry<float> configColorB;
        public static ConfigEntry<bool> sunLighting;
        public static ConfigEntry<bool> disableFestiveAirport;

        public static ConfigEntry<float> frontLeftIntensity;
        public static ConfigEntry<float> frontLeftSpotAngle;
        public static ConfigEntry<bool> frontLeftLightActive;

        public static ConfigEntry<float> frontRightIntensity;
        public static ConfigEntry<float> frontRightSpotAngle;
        public static ConfigEntry<bool> frontRightLightActive;

        public static bool isAirportRemixedLoaded = false;

        public static AssetBundle LightBundle;

        private Light frontLeftLight;
        private Light frontRightLight;
        private Renderer greenScreenRenderer1;
        private Renderer greenScreenRenderer2;
        private Renderer greenScreenRenderer3;
        private Renderer greenScreenRenderer4;

        private GameObject holidayAirport;

        private string CurrentScene = "";

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            DontDestroyOnLoad(this);

            CheckMod("tony4twentys.Airport_Remixed", ref isAirportRemixedLoaded);

            if (isAirportRemixedLoaded)
            {
                Logger.LogInfo("Airport Remixed is loaded, applying compatibility settings.");
            }
            else
            {
                Logger.LogInfo("Airport Remixed is not loaded. Resume normal behavior.");
            }

            configColorR = Config.Bind("General", "ColorR", 0.0f, "Red value for custom color (0-255).");
            configColorG = Config.Bind("General", "ColorG", 255.0f, "Green value for custom color (0-255).");
            configColorB = Config.Bind("General", "ColorB", 0.0f, "Blue value for custom color (0-255).");
            sunLighting = Config.Bind("General", "SunLighting", true, "If set to false, in the airport the sun won't have any lighting effects, good for getting that perfect shot.");
            disableFestiveAirport = Config.Bind("General", "Disable Festive Airport", false, "If set to true, disables the festive decorations in the airport (if they are present), this can be toggled in game.");

            frontLeftIntensity = Config.Bind("Front Left Light", "FL Intensity", 1.0f, "Intensity of the front left light.");
            frontLeftSpotAngle = Config.Bind("Front Left Light", "FL SpotAngle", 120f, "Spot angle of the front left light.");
            frontLeftLightActive = Config.Bind("Front Left Light", "FL LightActive", true, "If set to false, the front left light will be disabled.");

            frontRightIntensity = Config.Bind("Front Right Light", "FR Intensity", 1.0f, "Intensity of the front right light.");
            frontRightSpotAngle = Config.Bind("Front Right Light", "FR SpotAngle", 120f, "Spot angle of the front right light.");
            frontRightLightActive = Config.Bind("Front Right Light", "FR LightActive", true, "If set to false, the front right light will be disabled.");

            configColorR.SettingChanged += OnConfigSettingChanged;
            configColorG.SettingChanged += OnConfigSettingChanged;
            configColorB.SettingChanged += OnConfigSettingChanged;
            disableFestiveAirport.SettingChanged += OnConfigSettingChanged;
            frontLeftIntensity.SettingChanged += OnConfigSettingChanged;
            frontLeftSpotAngle.SettingChanged += OnConfigSettingChanged;
            frontLeftLightActive.SettingChanged += OnConfigSettingChanged;
            frontRightIntensity.SettingChanged += OnConfigSettingChanged;
            frontRightSpotAngle.SettingChanged += OnConfigSettingChanged;
            frontRightLightActive.SettingChanged += OnConfigSettingChanged;

            configColorR.Value = Mathf.Clamp(configColorR.Value, 0f, 255f);
            configColorG.Value = Mathf.Clamp(configColorG.Value, 0f, 255f);
            configColorB.Value = Mathf.Clamp(configColorB.Value, 0f, 255f);

            sunLighting.SettingChanged += (sender, args) =>
            {
                if (GameObject.Find("BL_Holiday"))
                {
                    GameObject day = GameObject.Find("SpecialDay Airport (1)");
                    day.transform.Find("Directional Light").gameObject.SetActive(sunLighting.Value);
                }
                else
                {
                    GameObject day = GameObject.Find("SpecialDay Airport");
                    day.transform.Find("Directional Light").gameObject.SetActive(sunLighting.Value);
                }
            };

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LightBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "lightbundle.atomic"));
            if (LightBundle == null)
            {
                Logger.LogError("Failed to load the light bundle!!");
                return;
            }
            else
            {
                Logger.LogMessage("Light bundle loaded successfully.");
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            CurrentScene = scene.name;
            if (scene.name == "Airport")
            {
                if (GameObject.Find("BL_Holiday"))
                {
                    GameObject day = GameObject.Find("SpecialDay Airport (1)");
                    day.transform.Find("Directional Light").gameObject.SetActive(sunLighting.Value);

                    holidayAirport = GameObject.Find("BL_Holiday");
                    holidayAirport.SetActive(!disableFestiveAirport.Value);
                }
                else
                {
                    GameObject day = GameObject.Find("SpecialDay Airport");
                    day.transform.Find("Directional Light").gameObject.SetActive(sunLighting.Value);
                }

                Material material = new Material(Shader.Find("Unlit/Color"));
                Material material2 = new Material(Shader.Find("W/Peak_Standard"));

                if (!isAirportRemixedLoaded)
                {
                    greenScreenRenderer1 = CreateCube(new Vector3(-6.3764f, 3.94f, 122.8569f), new Vector3(0.0073f, 6.9709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart1");
                    greenScreenRenderer2 = CreateCube(new Vector3(-6.3764f, -1.76f, 122.4569f), new Vector3(16.6818f, 4.6709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart2");
                    greenScreenRenderer3 = CreateCube(new Vector3(-6.3764f, 3.94f, 122.8669f), new Vector3(0.0073f, 6.9709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material2, "GreenScreenPart3");
                }
                else
                {
                    greenScreenRenderer1 = CreateCube(new Vector3(-27.8256f, 8.2482f, 48.777f), new Vector3(0.0073f, 15.1564f, 22.1709f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart1");
                    greenScreenRenderer2 = CreateCube(new Vector3(-27.6983f, -1.44f, 41.3118f), new Vector3(14.9472f, 4.6709f, 21.911f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart2");
                    greenScreenRenderer3 = CreateCube(new Vector3(-27.8256f, 8.2482f, 48.778f), new Vector3(0.0073f, 15.1564f, 22.1709f), Quaternion.Euler(0f, 90f, 0f), material2, "GreenScreenPart3");
                    greenScreenRenderer4 = CreateCube(new Vector3(-38.6728f, 8.2482f, 44.9271f), new Vector3(0.4073f, 15.1564f, 22.1709f), Quaternion.Euler(0f, 0f, 0f), material, "GreenScreenPart4");
                }

                Destroy(GameObject.Find("fence (17)"));
                Destroy(GameObject.Find("fence (16)"));
                Destroy(GameObject.Find("fence (15)"));
                Destroy(GameObject.Find("fence (14)"));

                Material lightMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                Texture2D lightTexture = LightBundle.LoadAsset<Texture2D>("LightTexture");

                if (lightTexture != null)
                {
                    lightMaterial.SetTexture("_BaseMap", lightTexture);
                    Logger.LogMessage("LightTexture loaded and applied to material successfully.");
                }
                else
                {
                    Logger.LogError("Failed to load LightTexture from the asset bundle.");
                }

                if (!isAirportRemixedLoaded)
                {
                    frontLeftLight = CreateLight(new Vector3(-2.0073f, 1.7926f, 113.8106f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 50.3132f, 180f), "GreenScreenLight1", lightMaterial);
                    frontRightLight = CreateLight(new Vector3(-10.6891f, 1.7926f, 113.8106f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 130.4433f, 180f), "GreenScreenLight2", lightMaterial);
                }
                else
                {
                    frontLeftLight = CreateLight(new Vector3(-20.8874f, 2.0162f, 33.4678f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 50.3132f, 180f), "GreenScreenLight1", lightMaterial);
                    frontRightLight = CreateLight(new Vector3(-34.7994f, 2.0226f, 33.4846f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 130.4433f, 180f), "GreenScreenLight2", lightMaterial);
                }
                
                UpdateObjects();
            }
        }

        private void OnConfigSettingChanged(object sender, EventArgs e)
        {
            configColorR.Value = Mathf.Clamp(configColorR.Value, 0f, 255f);
            configColorG.Value = Mathf.Clamp(configColorG.Value, 0f, 255f);
            configColorB.Value = Mathf.Clamp(configColorB.Value, 0f, 255f);

            if (sender == disableFestiveAirport && holidayAirport != null)
            {
                holidayAirport.SetActive(!disableFestiveAirport.Value);
            }

            if (greenScreenRenderer1 != null && frontLeftLight != null)
            {
                UpdateObjects();
            }
        }

        private void UpdateObjects()
        {
            Color customColor = new Color(configColorR.Value / 255f, configColorG.Value / 255f, configColorB.Value / 255f);
            greenScreenRenderer1.material.color = customColor;
            greenScreenRenderer1.material.SetColor("_EmissionColor", customColor * 10f);
            greenScreenRenderer2.material.color = customColor;
            greenScreenRenderer2.material.SetColor("_EmissionColor", customColor * 10f);
            greenScreenRenderer3.material.SetColor("_BaseColor", customColor);

            if (greenScreenRenderer4 != null)
            {
                greenScreenRenderer4.material.color = customColor;
                greenScreenRenderer4.material.SetColor("_EmissionColor", customColor * 10f);
            }

            if (frontLeftLight != null)
            {
                frontLeftLight.gameObject.SetActive(frontLeftLightActive.Value);
                frontLeftLight.intensity = frontLeftIntensity.Value;
                frontLeftLight.spotAngle = frontLeftSpotAngle.Value;
            }

            if (frontRightLight != null)
            {
                frontRightLight.gameObject.SetActive(frontRightLightActive.Value);
                frontRightLight.intensity = frontRightIntensity.Value;
                frontRightLight.spotAngle = frontRightSpotAngle.Value;
            }
        }

        private Light CreateLight(Vector3 pos, Vector3 scale, Quaternion rotation, string Name, Material material)
        {
            GameObject lightAsset = LightBundle.LoadAsset<GameObject>("GreenScreenLight");
            if (lightAsset == null)
            {
                Logger.LogError($"Failed to load {Name} from bundle.");
                return null;
            }

            GameObject light = Instantiate(lightAsset);
            MeshRenderer lightRenderer = light.GetComponent<MeshRenderer>();
            MeshCollider meshCollider = light.AddComponent<MeshCollider>();

            if (material != null)
            {
                lightRenderer.material = material;
            }

            light.transform.position = pos;
            light.transform.localScale = scale;
            light.transform.rotation = rotation;
            light.name = Name;

            meshCollider.sharedMesh = light.GetComponent<MeshFilter>().sharedMesh;
            meshCollider.convex = true;

            Transform actualLightTransform = light.transform.Find("Spot Light");
            if (actualLightTransform != null)
            {
                actualLightTransform.localEulerAngles = new Vector3(0f, 90f, 0f);
                Light actualLight = actualLightTransform.GetComponent<Light>();
                if (actualLight != null)
                {
                    return actualLight;
                }
            }
            return null;
        }

        private Renderer CreateCube(Vector3 pos, Vector3 scale, Quaternion rotation, Material material, string Name)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = material;

            part.transform.position = pos;
            part.transform.localScale = scale;
            part.transform.rotation = rotation;

            part.name = Name;
            return renderer;
        }

        public void CheckMod(string modGUID, ref bool loaded)
        {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(modGUID, out var pluginInfo);
            loaded = pluginInfo != null;
        }
    }
}