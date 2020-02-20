///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

var artifactsDir = Directory("./artifacts");
var packages = "./artifacts/packages";
var solutionPath = "./Curiosity.SPSS.sln";
var framework = "netstandard2.0";

var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals("master",
    BuildSystem.TravisCI.Environment.Build.Branch);

var nugetSource = "https://api.nuget.org/v3/index.json";


Task("Clean")
    .Does(() => 
    {            
        DotNetCoreClean(solutionPath);        
        DirectoryPath[] cleanDirectories = new DirectoryPath[] {
            artifactsDir
        };
    
        CleanDirectories(cleanDirectories);
    
        foreach(var path in cleanDirectories) { EnsureDirectoryExists(path); }
    
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        var settings = new DotNetCoreBuildSettings
          {
              Configuration = configuration
          };
          
        DotNetCoreBuild(
            solutionPath,
            settings);
    });

Task("Tests")
    .IsDependentOn("Build")
    .Does(() =>
    {        
        Information("UnitTests task...");
        var projects = GetFiles("./tests/**/*csproj");
        foreach(var project in projects)
        {
            Information(project);
            
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = false
                });
        }
    });
    
Task("Pack")
    .WithCriteria(isMasterBranch)
    .Does(() =>
    {        
         Information("Packing to nupkg...");
         var settings = new DotNetCorePackSettings
          {
              Configuration = configuration,
              OutputDirectory = packages
          };
         
          DotNetCorePack(solutionPath, settings);
    });
    
Task("Publish")
    .IsDependentOn("Pack")
    .WithCriteria(isMasterBranch)
    .Does(() => {
    
        var nugetApiKey = EnvironmentVariable("nugetApiKey");
        if (string.IsNullOrEmpty(nugetApiKey))
            throw new Exception("No NUGET API key specified");
        var pushSettings = new DotNetCoreNuGetPushSettings 
        {
            Source = nugetSource,
            ApiKey = nugetApiKey
        };
        Information(packages);
        var pkgs = GetFiles($"{packages}/*.nupkg");
        foreach(var pkg in pkgs) 
        {
            Information($"Publishing \"{pkg}\".");
            DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
        }
    });

 
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Tests");
 
Task("Master")
    .IsDependentOn("Build")
    .IsDependentOn("Tests")
    .IsDependentOn("Pack");
    .IsDependentOn("Publish");

RunTarget(target);

