<# Starts the ProcessCreation and PuppetMasterGUI processes #>
Start-Process -FilePath "DADSTORM\ProcessCreation\bin\Debug\ProcessCreation.exe" -WorkingDirectory "DADSTORM\ProcessCreation\bin\Debug\"
Start-Process -FilePath "DADSTORM\PuppetMasterGUI\bin\Debug\PuppetMasterGUI.exe" -WorkingDirectory "DADSTORM\PuppetMasterGUI\bin\Debug\"