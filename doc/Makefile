# nmake file for building the DocFX API documentation

gh-pages: doc
	-robocopy _site ..\docs /mir /njh /ns /nc /nfl /ndl /np

doc: DocFx.Plugins.PlantUml
	docfx docfx.json

DocFx.Plugins.PlantUml:
	nuget install DocFx.Plugins.PlantUml -ExcludeVersion -OutputDirectory .

clean:
	-del /s /q _site\*
	-del /s /q api\*
