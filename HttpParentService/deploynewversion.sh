version=1.0.16
# build
docker build --tag nvhoanganh1909/azurefuncdockernewrelicparentlinux:v$version . -f DockerfileLinux

# push
docker push nvhoanganh1909/azurefuncdockernewrelicparentlinux:v$version

# update image - New Relic
kubectl set image deployment/azfunchttpexampleparent azfunchttpexampleparent=nvhoanganh1909/azurefuncdockernewrelicparentlinux:v$version -n azfuncdemo
