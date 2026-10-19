"""Reference deployment helper. Student copies/adapts this to their own repository.
Receives service frontend|api and immutable IMAGE_NAME; creates or replaces that app's
configuration. This helper owns that configuration, so add custom env/settings here.
"""
import json, os, subprocess, sys, tempfile
from pathlib import Path

def main():
    service=sys.argv[1]
    if service not in ('frontend','api'):raise SystemExit('Choose frontend or api')
    e=os.environ
    required=['AZURE_SUBSCRIPTION_ID','RESOURCE_GROUP','CONTAINER_ENVIRONMENT','FRONTEND_APP','API_APP',
      'ACR_SERVER','RUNTIME_IDENTITY_ID','RUNTIME_CLIENT_ID','BLOB_SERVICE_URI','BLOB_CONTAINER','IMAGE_NAME']
    for key in required:
        if not e.get(key):raise SystemExit('Missing setting '+key)
    if service=='api' and not e.get('SQL_CONNECTION_STRING'):raise SystemExit('Missing SQL_CONNECTION_STRING secret')
    base=f'/subscriptions/{e["AZURE_SUBSCRIPTION_ID"]}/resourceGroups/{e["RESOURCE_GROUP"]}'
    name=e['FRONTEND_APP' if service=='frontend' else 'API_APP']
    identity=e['RUNTIME_IDENTITY_ID']
    env=[{'name':'ASPNETCORE_HTTP_PORTS','value':'8080'},
         {'name':'AZURE_CLIENT_ID','value':e['RUNTIME_CLIENT_ID']},
         {'name':'BlobServiceUri','value':e['BLOB_SERVICE_URI']},
         {'name':'BlobContainer','value':e['BLOB_CONTAINER']}]
    secrets=[]
    if service=='api':
        secrets=[{'name':'db-connection','value':e['SQL_CONNECTION_STRING']}]
        env.append({'name':'ConnectionStrings__DefaultConnection','secretRef':'db-connection'})
    # Public API supports browser-based frontends; student must implement auth/CORS deliberately.
    doc={'location':'eastus2','identity':{'type':'UserAssigned','userAssignedIdentities':{identity:{}}},
      'properties':{'environmentId':base+'/providers/Microsoft.App/managedEnvironments/'+e['CONTAINER_ENVIRONMENT'],
        'workloadProfileName':'Consumption',
        'configuration':{'activeRevisionsMode':'Single','maxInactiveRevisions':10,'secrets':secrets,
          'registries':[{'server':e['ACR_SERVER'],'identity':identity}],
          'ingress':{'external':True,'targetPort':8080,'transport':'auto','allowInsecure':False}},
        'template':{'containers':[{'name':service,'image':e['IMAGE_NAME'],
          'resources':{'cpu':0.25,'memory':'0.5Gi'},'env':env}],
          'scale':{'minReplicas':0,'maxReplicas':1}}}}
    url='https://management.azure.com'+base+'/providers/Microsoft.App/containerApps/'+name+'?api-version=2025-07-01'
    with tempfile.TemporaryDirectory() as folder:
        path=Path(folder)/'app.json';path.write_text(json.dumps(doc));path.chmod(0o600)
        # Capture output so ARM's secret-bearing response never enters CI logs.
        result=subprocess.run(['az','rest','--method','PUT','--url',url,'--body','@'+str(path),
           '--only-show-errors','--output','none'],capture_output=True,text=True)
        if result.returncode:
            error=result.stderr
            if e.get('SQL_CONNECTION_STRING'):error=error.replace(e['SQL_CONNECTION_STRING'],'[REDACTED]')
            raise SystemExit(error)
    print('Deployment submitted for '+name+'; inspect revision readiness before declaring success.')

if __name__=='__main__':main()
