#load nuget:https://www.myget.org/F/cake-contrib/api/v2?package=Cake.Recipe&prerelease

Environment.SetVariableNames();

BuildParameters.SetParameters(
    context: Context,
    buildSystem: BuildSystem,
    sourceDirectoryPath: "./src",
    title: "Cake.HgVersion",
    repositoryOwner: "vCipher",
    repositoryName: "Cake.HgVersion",
    appVeyorAccountName: "vCipher",
    shouldRunCodecov: false,
    shouldRunDupFinder: false,
    shouldRunInspectCode: false,
    solutionFilePath: "./src/Cake.HgVersion.sln");

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(
    context: Context,
    dupFinderExcludePattern: new string[] {
        BuildParameters.RootDirectoryPath + "/src/*Tests/**/*.cs",
        BuildParameters.RootDirectoryPath + "/src/**/*.AssemblyInfo.cs"
    });

Build.Run();