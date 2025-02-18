# nmake file for building the DocFX API documentation

gh-pages: doc
	-robocopy _site ..\docs /mir /njh /ns /nc /nfl /ndl /np

doc: DocFx.Plugins.PlantUml imagefiles
	docfx docfx.json

DocFx.Plugins.PlantUml:
	nuget install DocFx.Plugins.PlantUml -ExcludeVersion -OutputDirectory .

imagefiles:
	xcopy /d /y ..\test\DMediatR.Tests\output\remotes.Monolith.svg images
	xcopy /d /y ..\test\DMediatR.Tests\output\remotes.FourNodes.svg images
	xcopy /d /y ..\test\DMediatR.Tests\output\remotes.Iot.svg images
	xcopy /d /y ..\test\DMediatR.Tests\output\remotes.Iot2.svg images

clean:
	-del /s /q _site\*
	-del /s /q api\*
	-del /s /q  ..\docs\*
