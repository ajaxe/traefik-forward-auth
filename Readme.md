# Traefik Forward Auth

Helper application that adds a Cookie based login to Traefik hosted applications that otherwise do not have any authentication.

## Building docker image

Use the following command

```bash
docker build . -f build/Dockerfile --network=host --tag apogee-dev/traefik-forward-auth:local
```
