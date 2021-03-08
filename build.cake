///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

var artifactsDir = Directory("./artifacts");
var packages = "./artifacts/packages";
var solutionPath = "./Curiosity.SPSS.sln";
var framework = "netstandard2.0";

var nugetSource = "https://api.nuget.org/v3/index.json";
var nugetApiKey = Argument<string>("nugetApiKey", null);

var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals("master",
    BuildSystem.TravisCI.Environment.Build.Branch);
    
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

Task("UnitTests")
    .Does(() =>
    {        
        Information("UnitTests task...");
        var projects = GetFiles("./tests/UnitTests/**/*csproj");
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
     
Task("IntegrationTests")
    .Does(() =>
    {        
        Information("IntegrationTests task...");
		
        Information("Running docker...");
        StartProcess("docker-compose", "-f ./tests/IntegrationTests/env-compose.yml up -d");
		Information("Running docker completed");
		
        var projects = GetFiles("./tests/IntegrationTests/**/*csproj");
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
    })
    .Finally(() =>
    {  
        Information("Stopping docker...");
        StartProcess("docker-compose", "-f ./tests/IntegrationTests/env-compose.yml down");
        Information("Stopping docker completed");
    });  
    
Task("Pack")
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
    .Does(() =>
    {
        if (isMasterBranch)
        {
             var pushSettings = new DotNetCoreNuGetPushSettings 
             {
                 Source = nugetSource,
                 ApiKey = nugetApiKey,
                 SkipDuplicate = true
             };
             
             var pkgs = GetFiles($"{packages}/*.nupkg");
             foreach(var pkg in pkgs) 
             {     
                 Information($"Publishing \"{pkg}\".");
                 DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
             }
        }
        else
        {
            Error("Can't publish because publishing configured only for TravisCI");
        }
 }); 
 
Task("ForcePublish")
    .IsDependentOn("Pack")
    .Does(() =>
    {
         var pushSettings = new DotNetCoreNuGetPushSettings 
         {
             Source = nugetSource,
             ApiKey = nugetApiKey,
             SkipDuplicate = true
         };
         
         var pkgs = GetFiles($"{packages}/*.nupkg");
         foreach(var pkg in pkgs) 
         {     
             Information($"Publishing \"{pkg}\".");
             DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
         }
 }); 
    
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests");
    
Task("TravisCI")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish");
  
RunTarget(target);
