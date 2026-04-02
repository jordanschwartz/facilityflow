/// <reference path="./.sst/platform/config.d.ts" />

export default $config({
  app(input) {
    return {
      name: "facilityflow",
      removal: input?.stage === "production" ? "retain" : "remove",
      protect: ["production"].includes(input?.stage),
      home: "aws",
      providers: {
        aws: {
          profile: "oncall",
        },
      },
    };
  },
  async run() {
    // ---- Secrets ----
    const jwtSecret = new sst.Secret("JwtSecret");
    const stripeSecretKey = new sst.Secret("StripeSecretKey");
    const stripeWebhookSecret = new sst.Secret("StripeWebhookSecret");
    const geminiApiKey = new sst.Secret("GeminiApiKey");
    const openAiApiKey = new sst.Secret("OpenAiApiKey");

    // ---- ACM Certificates (manual DNS validation via Namecheap) ----

    // Certificate for frontend (CloudFront requires us-east-1)
    const usEast1 = new aws.Provider("UsEast1", { region: "us-east-1", profile: "oncall" });
    const frontendCert = new aws.acm.Certificate(
      "FrontendCert",
      {
        domainName: "jobs.oncallfm.com",
        validationMethod: "DNS",
      },
      { provider: usEast1 },
    );

    // Certificate for API (in default region)
    const apiCert = new aws.acm.Certificate("ApiCert", {
      domainName: "api.oncallfm.com",
      validationMethod: "DNS",
    });

    // Output the DNS validation records for Namecheap
    frontendCert.domainValidationOptions.apply((opts) => {
      for (const opt of opts) {
        console.log(`\n🔑 FRONTEND CERT — Add this CNAME at Namecheap:`);
        console.log(`   Host: ${opt.resourceRecordName}`);
        console.log(`   Value: ${opt.resourceRecordValue}`);
      }
    });

    apiCert.domainValidationOptions.apply((opts) => {
      for (const opt of opts) {
        console.log(`\n🔑 API CERT — Add this CNAME at Namecheap:`);
        console.log(`   Host: ${opt.resourceRecordName}`);
        console.log(`   Value: ${opt.resourceRecordValue}`);
      }
    });

    // Wait for certificate validation
    const frontendCertValidation = new aws.acm.CertificateValidation(
      "FrontendCertValidation",
      { certificateArn: frontendCert.arn },
      { provider: usEast1 },
    );

    const apiCertValidation = new aws.acm.CertificateValidation(
      "ApiCertValidation",
      { certificateArn: apiCert.arn },
    );

    // ---- VPC ----
    const vpc = new sst.aws.Vpc("Vpc", {
      bastion: true,
      nat: "ec2",
    });

    // ---- Database ----
    const database = new sst.aws.Postgres("Database", {
      vpc,
      scaling: {
        min: "0.5 ACU",
        max: "2 ACU",
      },
    });

    // ---- Backend API (ECS Fargate) ----
    const cluster = new sst.aws.Cluster("Cluster", { vpc });

    const api = cluster.addService("Api", {
      link: [database, jwtSecret, stripeSecretKey, stripeWebhookSecret, geminiApiKey, openAiApiKey],
      public: {
        domain: {
          name: "api.oncallfm.com",
          dns: false,
          cert: apiCertValidation.certificateArn,
        },
        ports: [{ listen: "443/https", forward: "8080/http" }],
      },
      image: {
        context: "./backend",
        dockerfile: "Dockerfile",
      },
      environment: {
        ASPNETCORE_URLS: "http://+:8080",
        ASPNETCORE_ENVIRONMENT: "Production",
        App__SeedDatabase: "true",
      },
      cpu: "0.25 vCPU",
      memory: "0.5 GB",
    });

    // ---- Frontend (Static Site) ----
    const frontend = new sst.aws.StaticSite("Frontend", {
      path: "./frontend",
      domain: {
        name: "jobs.oncallfm.com",
        dns: false,
        cert: frontendCertValidation.certificateArn,
      },
      build: {
        command: "npx vite build",
        output: "dist",
      },
      environment: {
        VITE_API_URL: "https://api.oncallfm.com",
      },
    });

    return {
      api: api.url,
      frontend: frontend.url,
    };
  },
});
