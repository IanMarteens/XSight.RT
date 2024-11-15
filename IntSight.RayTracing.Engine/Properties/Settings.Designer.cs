﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IntSight.RayTracing.Engine.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.1.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        /// <summary>
        /// Minimum effectivity required from bounding spheres.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Minimum effectivity required from bounding spheres.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.9")]
        public double BoundingSphereThreshold {
            get {
                return ((double)(this["BoundingSphereThreshold"]));
            }
            set {
                this["BoundingSphereThreshold"] = value;
            }
        }
        
        /// <summary>
        /// Maximum size of shape repeater for inline expansion.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Maximum size of shape repeater for inline expansion.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int LoopThreshold {
            get {
                return ((int)(this["LoopThreshold"]));
            }
            set {
                this["LoopThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int UnionThreshold {
            get {
                return ((int)(this["UnionThreshold"]));
            }
            set {
                this["UnionThreshold"] = value;
            }
        }
        
        /// <summary>
        /// Number of levels for tracing an additional ray with diffuse reflections.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Number of levels for tracing an additional ray with diffuse reflections.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int DiffusionLevels {
            get {
                return ((int)(this["DiffusionLevels"]));
            }
            set {
                this["DiffusionLevels"] = value;
            }
        }
        
        /// <summary>
        /// Background color for the Sonar sampler.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Background color for the Sonar sampler.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Black")]
        public global::System.Drawing.Color SonarBackColor {
            get {
                return ((global::System.Drawing.Color)(this["SonarBackColor"]));
            }
            set {
                this["SonarBackColor"] = value;
            }
        }
        
        /// <summary>
        /// Near color for the Sonar sampler.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Near color for the Sonar sampler.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("White")]
        public global::System.Drawing.Color SonarForeColor {
            get {
                return ((global::System.Drawing.Color)(this["SonarForeColor"]));
            }
            set {
                this["SonarForeColor"] = value;
            }
        }
        
        /// <summary>
        /// Far color for the Sonar sampler.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Far color for the Sonar sampler.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("64, 64, 64")]
        public global::System.Drawing.Color SonarFarColor {
            get {
                return ((global::System.Drawing.Color)(this["SonarFarColor"]));
            }
            set {
                this["SonarFarColor"] = value;
            }
        }
    }
}
