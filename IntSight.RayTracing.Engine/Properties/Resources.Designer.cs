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
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("IntSight.RayTracing.Engine.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Meridians.
        /// </summary>
        internal static string BenchmarkMeridians {
            get {
                return ResourceManager.GetString("BenchmarkMeridians", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sphlakes.
        /// </summary>
        internal static string BenchmarkSphlakes {
            get {
                return ResourceManager.GetString("BenchmarkSphlakes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Water.
        /// </summary>
        internal static string BenchmarkWater {
            get {
                return ResourceManager.GetString("BenchmarkWater", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Background image file not found: {0}..
        /// </summary>
        internal static string ErrorBitmapNotFound {
            get {
                return ResourceManager.GetString("ErrorBitmapNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} can not be used in intersections and differences..
        /// </summary>
        internal static string ErrorInvalidShapeInCsg {
            get {
                return ResourceManager.GetString("ErrorInvalidShapeInCsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There&apos;s no camera!.
        /// </summary>
        internal static string ErrorNoCamera {
            get {
                return ResourceManager.GetString("ErrorNoCamera", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There&apos;re no lights!.
        /// </summary>
        internal static string ErrorNoLights {
            get {
                return ResourceManager.GetString("ErrorNoLights", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There&apos;s no scene!.
        /// </summary>
        internal static string ErrorNoScene {
            get {
                return ResourceManager.GetString("ErrorNoScene", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0:F3} rows/second.
        /// </summary>
        internal static string MsgRowsBySecond {
            get {
                return ResourceManager.GetString("MsgRowsBySecond", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method {0} is not implemented..
        /// </summary>
        internal static string NotImplemented {
            get {
                return ResourceManager.GetString("NotImplemented", resourceCulture);
            }
        }
    }
}