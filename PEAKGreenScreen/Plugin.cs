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
    [BepInPlugin("PEAKGreenScreen", "Green Screen", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        public static ConfigEntry<float> configColorR;
        public static ConfigEntry<float> configColorG;
        public static ConfigEntry<float> configColorB;
        public static ConfigEntry<bool> sunLighting;

        public static ConfigEntry<float> frontLeftIntensity;
        public static ConfigEntry<float> frontLeftSpotAngle;
        public static ConfigEntry<bool> frontLeftLightActive;

        public static ConfigEntry<float> frontRightIntensity;
        public static ConfigEntry<float> frontRightSpotAngle;
        public static ConfigEntry<bool> frontRightLightActive;

        public static AssetBundle LightBundle;

        private Light frontLeftLight;
        private Light frontRightLight;
        private Renderer greenScreenRenderer1;
        private Renderer greenScreenRenderer2;
        private Renderer greenScreenRenderer3;

        private string CurrentScene = "";

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            DontDestroyOnLoad(this);

           
            configColorR = Config.Bind("General", "ColorR", 0.0f, "Red value for custom color (0-255).");
            configColorG = Config.Bind("General", "ColorG", 255.0f, "Green value for custom color (0-255).");
            configColorB = Config.Bind("General", "ColorB", 0.0f, "Blue value for custom color (0-255).");
            sunLighting = Config.Bind("General", "SunLighting", true, "If set to false, in the airport the sun won't have any lighting effects, good for getting that perfect shot.");

            frontLeftIntensity = Config.Bind("Front Left Light", "FL Intensity", 1.0f, "Intensity of the front left light.");
            frontLeftSpotAngle = Config.Bind("Front Left Light", "FL SpotAngle", 120f, "Spot angle of the front left light.");
            frontLeftLightActive = Config.Bind("Front Left Light", "FL LightActive", true, "If set to false, the front left light will be disabled.");

            frontRightIntensity = Config.Bind("Front Right Light", "FR Intensity", 1.0f, "Intensity of the front right light.");
            frontRightSpotAngle = Config.Bind("Front Right Light", "FR SpotAngle", 120f, "Spot angle of the front right light.");
            frontRightLightActive = Config.Bind("Front Right Light", "FR LightActive", true, "If set to false, the front right light will be disabled.");

            configColorR.SettingChanged += OnConfigSettingChanged;
            configColorG.SettingChanged += OnConfigSettingChanged;
            configColorB.SettingChanged += OnConfigSettingChanged;
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
                if (CurrentScene == "Airport")
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
                GameObject day = GameObject.Find("SpecialDay Airport");
                day.transform.Find("Directional Light").gameObject.SetActive(sunLighting.Value);

                Material material = new Material(Shader.Find("Unlit/Color"));
                Material material2 = new Material(Shader.Find("W/Peak_Standard"));

                greenScreenRenderer1 = CreateCube(new Vector3(-6.3764f, 3.94f, 122.8569f), new Vector3(0.0073f, 6.9709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart1");
                greenScreenRenderer2 = CreateCube(new Vector3(-6.3764f, -1.76f, 122.4569f), new Vector3(16.6818f, 4.6709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material, "GreenScreenPart2");
                greenScreenRenderer3 = CreateCube(new Vector3(-6.3764f, 3.94f, 122.8669f), new Vector3(0.0073f, 6.9709f, 15.1f), Quaternion.Euler(0f, 90f, 0f), material2, "GreenScreenPart3");

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

                frontLeftLight = CreateLight(new Vector3(-2.0073f, 1.7926f, 113.8106f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 50.3132f, 180f), "GreenScreenLight1", lightMaterial);
                frontRightLight = CreateLight(new Vector3(-10.6891f, 1.7926f, 113.8106f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Euler(0f, 130.4433f, 180f), "GreenScreenLight2", lightMaterial);

                UpdateObjects();
            }
        }

        private void OnConfigSettingChanged(object sender, EventArgs e)
        {
            configColorR.Value = Mathf.Clamp(configColorR.Value, 0f, 255f);
            configColorG.Value = Mathf.Clamp(configColorG.Value, 0f, 255f);
            configColorB.Value = Mathf.Clamp(configColorB.Value, 0f, 255f);
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
    }
}