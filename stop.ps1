<# Stops all of the Operator, PuppetMasterGUI and ProcessCreation processes #>

Stop-Process -processname Operator
Stop-Process -processname PuppetMasterGUI 
Stop-Process -processname ProcessCreation