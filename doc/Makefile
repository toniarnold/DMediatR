# nmake file for building the DocFX API documentation

gh-pages: doc
	-robocopy _site ..\gh-pages /mir /njh /ns /nc /nfl /ndl /np

doc:
	docfx docfx.json

clean:
	-del /s /q _site\*
	-del /s /q api\*
