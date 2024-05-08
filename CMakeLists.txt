cmake_minimum_required(VERSION 3.8)

project(MegEditor LANGUAGES CSharp)

include(CSharpUtilities)

add_executable(${PROJECT_NAME}
    src/App.config
    src/AboutBox.cs
    src/AboutBox.Designer.cs
    src/AboutBox.resx
    src/AskFormatForm.cs
    src/AskFormatForm.Designer.cs
    src/AskFormatForm.resx
    src/EncryptionKeys.cs
    src/MainForm.cs
    src/MainForm.Designer.cs
    src/MainForm.resx
    src/MegaFile.cs
    src/Program.cs
    src/Properties/AssemblyInfo.cs
    src/Properties/Resources.Designer.cs
    src/Properties/Resources.resx
    src/Properties/Settings.Designer.cs
    src/Properties/Settings.settings
)

csharp_set_windows_forms_properties(
    src/AboutBox.cs
    src/AboutBox.Designer.cs
    src/AboutBox.resx
    src/AskFormatForm.cs
    src/AskFormatForm.Designer.cs
    src/AskFormatForm.resx
    src/MainForm.cs
    src/MainForm.Designer.cs
    src/MainForm.resx
)

set_property(TARGET ${PROJECT_NAME} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.8")
set_property(TARGET ${PROJECT_NAME} PROPERTY VS_DOTNET_REFERENCES
    "Microsoft.CSharp"
    "System"
    "System.Core"
    "System.Xml.Linq"
    "System.Data.DataSetExtensions"
    "System.Data"
    "System.Deployment"
    "System.Drawing"
    "System.Windows.Forms"
    "System.Xml"
)
