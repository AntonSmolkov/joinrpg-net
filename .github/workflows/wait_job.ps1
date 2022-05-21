&"kubectl" "--%" "replace --force -f OctoMigrateJob.yaml"
if ($LastExitCode -ne 0) {exit $LastExitCode}
  

  echo "Waiting for Job/$MigrateJobKubeName to be completed or failed..."
  #Keep waiting while job is not completed or failed
  while ( $(&"kubectl" "--%" "get jobs $MigrateJobKubeName -o json" | ConvertFrom-Json -Depth 
99).status.conditions.type -inotmatch "^Complete$|^Failed$" ){
    if ($LastExitCode -ne 0) {exit $LastExitCode}
    
    #Check last event for error. Must throw an error if, for instance, namespace CPU quoata was exceeded
    $LastEventForJob = &"kubectl" "--%" "get events --sort-by=.metadata.creationTimestamp  --field-selector  
involvedObject.kind=Job,involvedObject.name=$MigrateJobKubeName -o jsonpath='{.items[-1:].message}'"
    if ($LastEventForJob -imatch "^'Error .*") { throw $LastEventForJob }

    #Start showing/following logs when it's possible
    #AIR-2191 - Only solution for utf-8 output in octopus logs. 
    try{[Proc.Tools.exec]::runCommand("kubectl", "logs --follow job/$MigrateJobKubeName")}catch{}
    Start-Sleep 3
    }

  
  #Fail octopus step if job is Failed
  if ($Matches[0] -eq 'Failed') {
  echo "Migration has failed. Migration's job pod info:"
  &"kubectl" "--%"  "get pods --selector=job-name=$MigrateJobKubeName -o=yaml"
  exit 1
  }
  if ($KubectlExitCode -ne 0) {exit $KubectlExitCode}
