﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebUiTests.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WebUiTests.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to /* Drop all non-system stored procs */
        ///DECLARE @name VARCHAR(128)
        ///DECLARE @SQL VARCHAR(254)
        ///
        ///SELECT @name = (SELECT TOP 1 [name] FROM sysobjects WHERE [type] = &apos;P&apos; AND category = 0 ORDER BY [name])
        ///
        ///WHILE @name is not null
        ///BEGIN
        ///    SELECT @SQL = &apos;DROP PROCEDURE [dbo].[&apos; + RTRIM(@name) +&apos;]&apos;
        ///    EXEC (@SQL)
        ///    PRINT &apos;Dropped Procedure: &apos; + @name
        ///    SELECT @name = (SELECT TOP 1 [name] FROM sysobjects WHERE [type] = &apos;P&apos; AND category = 0 AND [name] &gt; @name ORDER BY [name])
        ///END
        ///GO
        ///
        ////* Drop all v [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DbCleanupScript {
            get {
                return ResourceManager.GetString("DbCleanupScript", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ////****** Object:  Table [dbo].[DbResourceAssociationInfo]    Script Date: 06.05.2020 17:13:01 ******/
        ///SET ANSI_NULLS ON
        ///GO
        ///SET QUOTED_IDENTIFIER ON
        ///GO
        ///CREATE TABLE [dbo].[DbResourceAssociationInfo](
        ///	[Id] [bigint] IDENTITY(1,1) NOT NULL,
        ///	[TagInstanceId] [bigint] NOT NULL,
        ///	[ResourceId] [bigint] NOT NULL,
        /// CONSTRAINT [PK_DbResourceAssociationInfo] PRIMARY KEY CLUSTERED 
        ///(
        ///	[Id] ASC
        ///)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DbPreparationScript {
            get {
                return ResourceManager.GetString("DbPreparationScript", resourceCulture);
            }
        }
    }
}
