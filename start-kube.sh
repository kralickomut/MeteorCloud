#!/bin/bash

echo "🧹 Deleting all existing Kubernetes resources..."
kubectl delete all --all
kubectl delete ingress --all
kubectl delete hpa --all

echo "🚀 Building Docker images for all services..."
eval $(minikube docker-env)
docker build -t user-service:dev -f src/UserService/Dockerfile .
docker build -t auth-service:dev -f src/AuthService/Dockerfile .
docker build -t notification-service:dev -f src/NotificationService/Dockerfile .
docker build -t workspace-service:dev -f src/WorkspaceService/Dockerfile .
docker build -t link-service:dev -f src/LinkService/Dockerfile .
docker build -t metadata-service:dev -f src/MetadataService/Dockerfile .
docker build -t audit-service:dev -f src/AuditService/Dockerfile .
docker build -t file-service:dev -f src/FileService/Dockerfile .

echo "📦 Applying Kubernetes manifests..."
kubectl apply -k ./k8s

echo "🔐 Creating TLS secret for Ingress..."
mkdir -p tls
openssl req -x509 -nodes -days 365 \
  -newkey rsa:2048 \
  -keyout tls/tls.key \
  -out tls/tls.crt \
  -subj "/CN=localhost/O=MeteorCloud"

kubectl delete secret meteorcloud-tls --ignore-not-found
kubectl create secret tls meteorcloud-tls \
  --cert=tls/tls.crt \
  --key=tls/tls.key \
  --namespace default

echo "🌐 Enabling Ingress and tunnel..."
minikube addons enable ingress
kubectl apply -f ./k8s/ingress.yaml
kubectl apply -f ./k8s/mailhog-ingress.yaml

echo "🔄 Waiting for Ingress controller to be ready..."
kubectl rollout status deployment ingress-nginx-controller -n ingress-nginx

echo "📊 Enabling Metrics Server for autoscaling..."
minikube addons enable metrics-server

echo "⏳ Waiting for metrics-server to be ready..."
kubectl rollout status deployment metrics-server -n kube-system

echo "📈 Applying autoscalers for each service..."
kubectl apply -f ./k8s/auth-service/hpa.yaml
kubectl apply -f ./k8s/user-service/hpa.yaml
kubectl apply -f ./k8s/workspace-service/hpa.yaml
kubectl apply -f ./k8s/notification-service/hpa.yaml
kubectl apply -f ./k8s/audit-service/hpa.yaml
kubectl apply -f ./k8s/file-service/hpa.yaml
kubectl apply -f ./k8s/metadata-service/hpa.yaml
kubectl apply -f ./k8s/link-service/hpa.yaml

echo "🌍 Ingress is exposed at: https://localhost"
echo "🛡️  Please start the ingress tunnel manually (required for LoadBalancer services):"
echo "    👉 Run: minikube tunnel"