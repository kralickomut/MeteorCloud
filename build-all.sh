#!/bin/bash
eval $(minikube docker-env)

docker build -t user-service:dev -f src/UserService/Dockerfile .
docker build -t auth-service:dev -f src/AuthService/Dockerfile .
docker build -t notification-service:dev -f src/NotificationService/Dockerfile .
docker build -t workspace-service:dev -f src/WorkspaceService/Dockerfile .
docker build -t link-service:dev -f src/LinkService/Dockerfile .
docker build -t metadata-service:dev -f src/MetadataService/Dockerfile .
docker build -t audit-service:dev -f src/AuditService/Dockerfile .
docker build -t file-service:dev -f src/FileService/Dockerfile .
