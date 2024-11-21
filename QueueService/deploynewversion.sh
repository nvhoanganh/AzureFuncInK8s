version=1.0.17
# build
docker build --tag nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version . -f DockerfileLinux

# push
docker push nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version


# update image - New Relic
kubectl set image deployment/azfunchttpexamplequeueconsumer azfunchttpexamplequeueconsumer=nvhoanganh1909/azurefuncdockernewrelicqueuelinux:v$version -n azfuncdemo