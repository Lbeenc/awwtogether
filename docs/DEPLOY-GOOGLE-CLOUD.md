# Deploying AwwTogether to Google Cloud (domain stays at GoDaddy)

**How the pieces fit together**

```
Visitor ──► yourdomain.com (DNS at GoDaddy)
              │  A / AAAA / CNAME records point at Google
              ▼
         Cloud Run service "awwtogether"   ◄── Cloud Build rebuilds on every push to GitHub main
         (Dockerfile: React site + ASP.NET API, free HTTPS cert)
              │
              ▼
         Firestore "inquiries" collection (contact-form submissions)
```

Everything is done in the Google Cloud web console, so you don't need to install anything. The domain stays registered at GoDaddy; you only change its DNS records.

> Throughout, replace **`yourdomain.com`** with your real domain.

---

## 1. Create the Google Cloud project

1. Go to <https://console.cloud.google.com> and sign in.
2. Open the project picker at the top, choose **New project**, name it `awwtogether`, then **Create**.
3. **Billing:** Cloud Run requires a billing account, even though this site should stay in the free tier. Go to **Billing**, then link or create one. *You have to do this step yourself.*
4. **Set a budget alert** so there are no surprises. Go to **Billing → Budgets & alerts → Create budget**, set **$5/month**, and alert at 50%, 90% and 100%.

## 2. Create the Firestore database

1. Go to **Firestore**, then **Create database**.
2. Choose **Native mode**.
3. Set the Database ID to `(default)` (leave it as is).
4. Set the location to **`us-central1`** (or the multi-region `nam5`). Keep it close to the Cloud Run region in step 4.
5. Choose **production rules** (the app talks to Firestore from the server, so the public client rules don't matter), then **Create**.

The `inquiries` collection is created automatically when the first form is submitted.

## 3. Create the admin key secret

This key is what lets you read inquiries through `GET /api/inquiries`.

1. Go to **Security → Secret Manager**. Enable the API if asked, then **Create secret**.
2. Name it `aww-admin-key`.
3. For the value, use a long random string, for example from a password manager. Save it somewhere safe.

## 4. Deploy to Cloud Run from GitHub

1. Go to **Cloud Run**, then **Deploy container → Service**.
2. Choose **Continuously deploy from a repository (source or function)**, then **Set up with Cloud Build**:
   - Provider: **GitHub**. Authenticate, then pick the repository **`Lbeenc/awwtogether`**.
   - Branch: `^main$`
   - Build type: **Dockerfile**, with source location `/Dockerfile`
3. Fill in the service settings:
   - **Service name:** `awwtogether`
   - **Region:** **`us-central1`**. This matters: custom domains only work in some regions, and us-central1 is one of them.
   - **Authentication:** **Allow unauthenticated invocations**, because it's a public website.
   - **Billing:** Request-based. CPU is only allocated while handling requests, which is the cheapest option.
   - **Scaling:** Min instances `0`, Max instances `3`.
4. Under **Container(s), Volumes, Networking, Security**:
   - **Container port:** `8080` (the default).
   - **Variables & Secrets → Reference a secret:**
     - Secret: `aww-admin-key`
     - Version: latest
     - Exposed as an **environment variable** named **`Admin__ApiKey`** (with two underscores).
   - You don't need to set `Storage__Provider`; the Dockerfile already sets it to `Firestore`.
5. Click **Create**. The first build takes about 5–8 minutes. When it finishes, you'll get a URL like `https://awwtogether-xxxxx-uc.a.run.app`.

## 5. Give the service permission to use Firestore and the secret

1. In Cloud Run, open the `awwtogether` service, go to the **Security** tab, and note the **Service account**. It's usually `PROJECT_NUMBER-compute@developer.gserviceaccount.com`.
2. Go to **IAM & Admin → IAM → Grant access**. Paste that service account and add both roles:
   - **Cloud Datastore User**, for reading and writing Firestore
   - **Secret Manager Secret Accessor**, for reading the admin key
3. Save. If step 4 failed with a secret-permission error, redeploy now: open the service, choose **Edit & deploy new revision**, then **Deploy**.

## 6. Test the `run.app` URL

- Open the URL. The site should load with your content.
- Submit the contact form, then check **Firestore → Data → `inquiries`**. Your test entry should be there.
- Optionally, list inquiries through the API:

  ```bash
  curl -H "X-Admin-Key: YOUR_SECRET" https://awwtogether-xxxxx-uc.a.run.app/api/inquiries
  ```

## 7. Connect your GoDaddy domain

### 7a. Map the domain in Google Cloud

1. In **Cloud Run**, click **Manage custom domains** (the "Domain mappings" link at the top), then **Add mapping**.
2. Select the service `awwtogether`.
3. Choose your domain. If it isn't verified yet, choose **Verify a new domain**. Google opens Search Console and gives you a **TXT record** to add.
4. **Add the TXT record in GoDaddy:**
   - Go to **My Products → your domain → DNS → Add New Record**.
   - Set **Type** to `TXT`, **Name** to `@`, and **Value** to the `google-site-verification=…` string Google gave you.
   - Save. Then click **Verify** in Search Console. It can take a few minutes for the record to be seen.
5. Back in Cloud Run, map **both** `yourdomain.com` and `www.yourdomain.com` to the service.
6. Google then shows the **DNS records** to add. Keep that screen open.

### 7b. Add the DNS records in GoDaddy

In GoDaddy, go to **DNS → DNS Records** for your domain:

1. **Remove anything that currently points the site elsewhere.**
   - Delete the existing `A` record for `@`. It's often called "Parked" or points to GoDaddy's website builder.
   - Delete the `CNAME` record for `www`, or edit it as shown in the table below.
   - If the domain has **Forwarding** set up (under **Domain → Forwarding**), remove it.
   - **Don't touch `MX` or email-related `TXT` records** if you use email on this domain.
2. **Add exactly the records Google showed you.** They normally look like this:

| Type | Name | Value |
| --- | --- | --- |
| A | @ | 216.239.32.21 |
| A | @ | 216.239.34.21 |
| A | @ | 216.239.36.21 |
| A | @ | 216.239.38.21 |
| AAAA | @ | 2001:4860:4802:32::15 |
| AAAA | @ | 2001:4860:4802:34::15 |
| AAAA | @ | 2001:4860:4802:36::15 |
| AAAA | @ | 2001:4860:4802:38::15 |
| CNAME | www | ghs.googlehosted.com |

   If Google's screen shows different values, use Google's.

3. Leave GoDaddy's **nameservers** as they are. You're only editing records.

### 7c. Wait for HTTPS

Google issues a free SSL certificate automatically once DNS has updated. This usually takes 15–60 minutes and can take up to 24 hours. The Cloud Run **Domain mappings** page shows a green check when it's done. Until then, `https://yourdomain.com` may show a certificate warning. That's normal while the certificate is being issued.

---

## Day-to-day

- **Update the site:** commit and `git push`. Cloud Build rebuilds and Cloud Run switches to the new version with no downtime. Watch progress under **Cloud Build → History**.
- **Edit text or prices:** change `server/AwwTogether.Api/Data/content.json`, then commit and push.
- **Read inquiries:** use **Firestore → Data → inquiries**, or the `curl` command above with your admin key.
- **Logs:** go to **Cloud Run → awwtogether → Logs**.
- **Roll back:** go to **Cloud Run → awwtogether → Revisions**, pick an older revision, and choose **Manage traffic → 100%**.

## What it should cost

These are the expected costs at portfolio-site traffic, as of late 2026. Check Google's pricing pages for current numbers.

- **Cloud Run:** the free tier covers 2 million requests a month. With min instances at 0, you pay nothing while nobody is visiting.
- **Firestore:** the free tier covers 20k writes and 50k reads a day, which is far more than a contact form needs.
- **Cloud Build and Artifact Registry:** both have free tiers that easily cover a few deploys a week. You can delete old images under Artifact Registry occasionally.
- **Domain:** you keep paying GoDaddy for registration as before.

The budget alert from step 1 warns you if anything changes.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| Build fails in Cloud Build | Open **Cloud Build → History**, click the failed build, and read the log. Most often a typo was pushed. Run `npm run build` and `dotnet test` locally first. |
| Site loads but the form says "Something went wrong" | The service account is missing **Cloud Datastore User** (step 5), or Firestore wasn't created (step 2). Check **Cloud Run → Logs**. |
| `GET /api/inquiries` returns 404 | `Admin__ApiKey` isn't set. Check the secret reference in step 4. |
| Domain shows a GoDaddy page | The old `A @` record or Forwarding is still in place (step 7b). |
| Certificate not ready after 24h | Check that the DNS records match Google's exactly. Use <https://dnschecker.org> to see what the world sees. |
