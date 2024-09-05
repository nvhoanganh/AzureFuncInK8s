version=1.0.13
# build
docker build --tag nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v$version . -f DockerfileLinux
docker build --tag nvhoanganh1909/azurefuncdockerailinux:v$version . -f DockerfileLinuxAI

# push
docker push nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v$version
docker push nvhoanganh1909/azurefuncdockerailinux:v$version


# update image - New Relic

kubectl set image deployment/azfunchttpexample azfunchttpexample=nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v$version -n azfuncdemo
kubectl set image deployment/azfunchttpexampleparent azfunchttpexampleparent=nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v$version -n azfuncdemo

# update image - AI 

kubectl set image deployment/azfunchttpexampleai azfunchttpexampleai=nvhoanganh1909/azurefuncdockerailinux:v$version -n azfuncdemoai
kubectl set image deployment/azfunchttpexampleparentai azfunchttpexampleparentai=nvhoanganh1909/azurefuncdockerailinux:v$version -n azfuncdemoai