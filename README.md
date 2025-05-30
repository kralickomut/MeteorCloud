# Návod ke spuštění aplikace Meteor Cloud

Tento návod slouží k plnohodnotnému spuštění aplikace Meteor Cloud, která je součástí bakalářské práce. Postupujte podle následujících kroků:

---

## 1. Požadavky

Před spuštěním aplikace je nutné mít nainstalováno:

- [Node.js](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli)
- [Docker](https://www.docker.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Minikube](https://minikube.sigs.k8s.io/)

---

## 2. Spuštění frontendové aplikace

1. Otevřete terminál a přejděte do složky `frontend`:
    ```bash
    cd frontend
    ```

2. Spusťte vývojový server Angular:
    ```bash
    ng serve
    ```

Frontend bude dostupný na adrese [http://localhost:4200](http://localhost:4200).

---

## 3. Spuštění backendové části pomocí Kubernetes

1. Ujistěte se, že máte spuštěný Docker Desktop a Minikube.

2. Spusťte Minikube:
    ```bash
    minikube start
    ```

3. V kořenovém adresáři projektu spusťte připravený skript `start-kube.sh`, který sestaví všechny služby a nasadí je do Minikube:
    ```bash
    ./start-kube.sh
    ```

4. Po úspěšném nasazení spusťte následující příkaz, abyste povolili přístup do clusteru:
    ```bash
    minikube tunnel
    ```

---

## 4. Přístup k aplikaci

- Otevřete webový prohlížeč a přejděte na adresu: [https://localhost](https://localhost)
- Při prvním spuštění je třeba schválit výjimku pro self-signed certifikát.
- Frontend aplikace je dostupný na adrese: [https://localhost:4200](https://localhost:4200)

---

## 5. Testování odesílání e-mailů

Pro simulaci odesílání e-mailů je součástí aplikace nástroj [MailHog](https://github.com/mailhog/MailHog):

- V prohlížeči otevřete adresu [http://mailhog.local](http://mailhog.local)
- Zde můžete sledovat všechny e-maily odeslané aplikací přes SMTP.

---

## Poznámky

- Pokud dojde ke změnám ve službách nebo kódu, spusťte skript `start-kube.sh` znovu pro přebuildování a nasazení.
- Aplikace komunikuje skrze TLS, proto je nutné mít otevřený `minikube tunnel`.

---

*V případě dotazů kontaktujte autora této práce.*