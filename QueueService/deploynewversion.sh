version=1.0.14
# build
docker build --tag nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version . -f DockerfileLinux
docker build --tag nvhoanganh1909/azurefuncdockeraiqueuelinux:v$version . -f DockerfileLinuxAI

# push
docker push nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version
docker push nvhoanganh1909/azurefuncdockeraiqueuelinux:v$version


# # update image - New Relic

# kubectl set image deployment/azfunchttpexample azfunchttpexample=nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version -n azfuncdemo
# kubectl set image deployment/azfunchttpexampleparent azfunchttpexampleparent=nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version -n azfuncdemo

# # update image - AI 

# kubectl set image deployment/azfunchttpexampleai azfunchttpexampleai=nvhoanganh1909/azurefuncdockeraiqueuelinux:v$version -n azfuncdemoai
# kubectl set image deployment/azfunchttpexampleparentai azfunchttpexampleparentai=nvhoanganh1909/azurefuncdockeraiqueuelinux:v$version -n azfuncdemoai