version=1.0.16
# build
docker build --tag nvhoanganh1909/azurefuncdockernewrelicchildlinux:v$version . -f DockerfileLinux

# push
docker push nvhoanganh1909/azurefuncdockernewrelicchildlinux:v$version

# update image - New Relic
kubectl set image deployment/azfunchttpexample azfunchttpexample=nvhoanganh1909/azurefuncdockernewrelicchildlinux:v$version -n azfuncdemo
