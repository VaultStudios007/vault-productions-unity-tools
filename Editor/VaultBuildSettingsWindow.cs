using System.IO;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VaultProductions.Tools.Editor
{
    public sealed class VaultBuildSettingsPackageWindow : EditorWindow
    {
        const string SettingsDir = "Assets/VaultProductions/RuntimeDebugFramework/Runtime/Resources/RuntimeDebugFramework";
        const string SettingsPath = SettingsDir + "/RuntimeDebugFrameworkSettings.asset";

        string _versionName;
        int _versionCode;
        bool _productionBuild;
        bool _buildAppBundle;
        bool _enableRuntimeDebugger;
        bool _useCustomKeystore;
        bool _keyPasswordSameAsKeystore;
        string _keystorePath;
        string _keystorePassword;
        string _keyAlias;
        string _keyPassword;
        UnityEngine.Object _settings;

        [MenuItem("Tools/Vault Productions/Build Settings (Package)", false, 1)]
        public static void Open()
        {
            var window = GetWindow<VaultBuildSettingsPackageWindow>("Vault Build Settings");
            window.minSize = new Vector2(520, 520);
            window.RefreshFromProject();
            window.Show();
        }

        void OnEnable()
        {
            RefreshFromProject();
        }

        void RefreshFromProject()
        {
            _versionName = PlayerSettings.bundleVersion;
            _versionCode = PlayerSettings.Android.bundleVersionCode;
            _productionBuild = !EditorUserBuildSettings.development;
            _buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            _useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            _keystorePath = PlayerSettings.Android.keystoreName;
            _keystorePassword = PlayerSettings.Android.keystorePass;
            _keyAlias = PlayerSettings.Android.keyaliasName;
            _keyPassword = PlayerSettings.Android.keyaliasPass;
            _keyPasswordSameAsKeystore = !string.IsNullOrEmpty(_keystorePassword) && _keystorePassword == _keyPassword;
            _settings = LoadOrCreateRuntimeDebugSettings();
            _enableRuntimeDebugger = ReadBoolSetting("EnableFramework", false) &&
                                     ReadBoolSetting("AutoCreateConsole", false) &&
                                     (_productionBuild ? ReadBoolSetting("EnableInReleaseBuilds", false) : true);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Vault Production Build Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this before every Android build. It updates the app version, Android version code, Google Play AAB option, and your in-game Android debug logger toggle.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            DrawVersionSection();
            EditorGUILayout.Space(8);
            DrawBuildModeSection();
            EditorGUILayout.Space(8);
            DrawSigningSection();
            EditorGUILayout.Space(8);
            DrawRuntimeDebuggerSection();
            EditorGUILayout.Space(12);
            DrawActions();
        }

        void DrawVersionSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);
            _versionName = EditorGUILayout.TextField(new GUIContent("Version Name", "PlayerSettings.bundleVersion, shown as version name in Google Play."), _versionName);
            _versionCode = EditorGUILayout.IntField(new GUIContent("Build Number", "Android version code. Must be higher than the last uploaded Play Store build."), _versionCode);
            EditorGUILayout.EndVertical();
        }

        void DrawBuildModeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Build Mode", EditorStyles.boldLabel);
            _productionBuild = EditorGUILayout.ToggleLeft(
                new GUIContent("Production Build", "Turns off Unity Development Build and script debugging. Use this for Play Store AAB builds."),
                _productionBuild);
            _buildAppBundle = EditorGUILayout.ToggleLeft(
                new GUIContent("Build Android App Bundle (AAB)", "Google Play requires an AAB for normal releases."),
                _buildAppBundle);
            EditorGUILayout.EndVertical();
        }

        void DrawSigningSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Android Signing", EditorStyles.boldLabel);

            _useCustomKeystore = EditorGUILayout.ToggleLeft(
                new GUIContent("Use Custom Keystore", "Use your release keystore for signed APK/AAB builds."),
                _useCustomKeystore);

            using (new EditorGUI.DisabledScope(!_useCustomKeystore))
            {
                EditorGUILayout.BeginHorizontal();
                _keystorePath = EditorGUILayout.TextField(
                    new GUIContent("Keystore Path", "Path to the release keystore file."),
                    _keystorePath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    var selected = EditorUtility.OpenFilePanel("Select Android Keystore", string.IsNullOrEmpty(_keystorePath) ? Application.dataPath : Path.GetDirectoryName(_keystorePath), "keystore,jks");
                    if (!string.IsNullOrEmpty(selected))
                        _keystorePath = selected;
                }
                EditorGUILayout.EndHorizontal();

                _keyAlias = EditorGUILayout.TextField(
                    new GUIContent("Key Alias", "Alias inside the keystore."),
                    _keyAlias);
                _keystorePassword = EditorGUILayout.PasswordField(
                    new GUIContent("Keystore Password", "Password for the keystore file."),
                    _keystorePassword);
                _keyPasswordSameAsKeystore = EditorGUILayout.ToggleLeft(
                    new GUIContent("Key Password Same As Keystore", "Most Unity release keys use the same password for keystore and key alias."),
                    _keyPasswordSameAsKeystore);

                using (new EditorGUI.DisabledScope(_keyPasswordSameAsKeystore))
                {
                    _keyPassword = EditorGUILayout.PasswordField(
                        new GUIContent("Key Password", "Password for the selected alias/key."),
                        _keyPasswordSameAsKeystore ? _keystorePassword : _keyPassword);
                }
            }

            if (_productionBuild && !_useCustomKeystore)
                EditorGUILayout.HelpBox("Production builds should use your release keystore, not the debug keystore.", MessageType.Warning);

            EditorGUILayout.EndVertical();
        }

        void DrawRuntimeDebuggerSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Android Runtime Logger", EditorStyles.boldLabel);
            _enableRuntimeDebugger = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Runtime Debugger Tool", "Controls the in-game Android logger UI/gesture/debug console."),
                _enableRuntimeDebugger);

            if (_productionBuild && _enableRuntimeDebugger)
            {
                EditorGUILayout.HelpBox(
                    "Debugger is enabled for a production build. Use this only for private testing builds, not Play Store production.",
                    MessageType.Warning);
            }
            else if (_productionBuild)
            {
                EditorGUILayout.HelpBox("Good for Play Store release: production build with runtime debugger disabled.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Height(32)))
                RefreshFromProject();

            GUI.enabled = CanApply();
            if (GUILayout.Button("Apply Settings", GUILayout.Height(32)))
                ApplySettings();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!CanApply())
                EditorGUILayout.HelpBox(GetValidationMessage(), MessageType.Error);
        }

        bool CanApply()
        {
            if (string.IsNullOrWhiteSpace(_versionName) || _versionCode <= 0)
                return false;

            if (!_useCustomKeystore)
                return true;

            return !string.IsNullOrWhiteSpace(_keystorePath) &&
                   !string.IsNullOrWhiteSpace(_keyAlias) &&
                   !string.IsNullOrEmpty(_keystorePassword) &&
                   !string.IsNullOrEmpty(_keyPasswordSameAsKeystore ? _keystorePassword : _keyPassword);
        }

        string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(_versionName))
                return "Version name cannot be empty.";
            if (_versionCode <= 0)
                return "Build number must be greater than 0.";
            if (!_useCustomKeystore)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(_keystorePath))
                return "Keystore path is required when custom keystore is enabled.";
            if (string.IsNullOrWhiteSpace(_keyAlias))
                return "Key alias is required when custom keystore is enabled.";
            if (string.IsNullOrEmpty(_keystorePassword))
                return "Keystore password is required when custom keystore is enabled.";
            if (string.IsNullOrEmpty(_keyPasswordSameAsKeystore ? _keystorePassword : _keyPassword))
                return "Key password is required when custom keystore is enabled.";
            return string.Empty;
        }

        void ApplySettings()
        {
            PlayerSettings.bundleVersion = _versionName.Trim();
            PlayerSettings.Android.bundleVersionCode = _versionCode;
            PlayerSettings.Android.useCustomKeystore = _useCustomKeystore;
            if (_useCustomKeystore)
            {
                PlayerSettings.Android.keystoreName = _keystorePath.Trim();
                PlayerSettings.Android.keystorePass = _keystorePassword;
                PlayerSettings.Android.keyaliasName = _keyAlias.Trim();
                PlayerSettings.Android.keyaliasPass = _keyPasswordSameAsKeystore ? _keystorePassword : _keyPassword;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.development = !_productionBuild;
            EditorUserBuildSettings.allowDebugging = !_productionBuild && _enableRuntimeDebugger;
            EditorUserBuildSettings.connectProfiler = !_productionBuild && _enableRuntimeDebugger;
            EditorUserBuildSettings.buildAppBundle = _buildAppBundle;

            _settings = LoadOrCreateRuntimeDebugSettings();
            if (_settings != null)
            {
                WriteBoolSetting("EnableFramework", _enableRuntimeDebugger);
                WriteBoolSetting("AutoCreateConsole", _enableRuntimeDebugger);
                WriteBoolSetting("EnableInReleaseBuilds", _productionBuild && _enableRuntimeDebugger);
                EditorUtility.SetDirty(_settings);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var mode = _productionBuild ? "Production" : "Development";
            var debugger = _enableRuntimeDebugger ? "enabled" : "disabled";
            Debug.Log($"[Vault Build Settings] Applied Android {mode} build: version {_versionName}, code {_versionCode}, AAB {_buildAppBundle}, runtime debugger {debugger}.");
            ShowNotification(new GUIContent("Vault build settings applied"));
        }

        static UnityEngine.Object LoadOrCreateRuntimeDebugSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SettingsPath);
            if (settings != null)
                return settings;

            var settingsType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("VaultProductions.RuntimeDebugFramework.LogSettings"))
                .FirstOrDefault(type => type != null);

            if (settingsType == null || !typeof(ScriptableObject).IsAssignableFrom(settingsType))
                return null;

            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            settings = CreateInstance(settingsType);
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        bool ReadBoolSetting(string propertyName, bool fallback)
        {
            if (_settings == null)
                return fallback;

            var serialized = new SerializedObject(_settings);
            var property = serialized.FindProperty(propertyName);
            return property != null ? property.boolValue : fallback;
        }

        void WriteBoolSetting(string propertyName, bool value)
        {
            if (_settings == null)
                return;

            var serialized = new SerializedObject(_settings);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.boolValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
