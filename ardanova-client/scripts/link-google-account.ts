import { Prisma, PrismaClient } from "@prisma/client";

import { assertLegacyGoogleLinkAllowed } from "../src/lib/google-account-link-guard";

const prisma = new PrismaClient();

async function linkLegacyGoogleAccount() {
  const target = assertLegacyGoogleLinkAllowed({
    ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK:
      process.env.ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK,
    LEGACY_LINK_USER_ID: process.env.LEGACY_LINK_USER_ID,
    LEGACY_LINK_GOOGLE_SUB: process.env.LEGACY_LINK_GOOGLE_SUB,
  });

  await prisma.$transaction(
    async (transaction) => {
      const user = await transaction.user.findUnique({
        where: { id: target.userId },
        select: { id: true },
      });
      if (!user)
        throw new Error("The requested persisted user does not exist.");

      const subjectOwner = await transaction.account.findUnique({
        where: {
          provider_providerAccountId: {
            provider: "google",
            providerAccountId: target.providerAccountId,
          },
        },
        select: { userId: true },
      });
      if (subjectOwner) {
        if (subjectOwner.userId === target.userId) return;
        throw new Error("The verified Google subject is already linked.");
      }

      const existingGoogleLink = await transaction.account.findFirst({
        where: { userId: target.userId, provider: "google" },
        select: { id: true },
      });
      if (existingGoogleLink) {
        throw new Error(
          "The persisted user already has a Google account link.",
        );
      }

      await transaction.account.create({
        data: {
          userId: target.userId,
          type: "oidc",
          provider: "google",
          providerAccountId: target.providerAccountId,
        },
      });
    },
    { isolationLevel: Prisma.TransactionIsolationLevel.Serializable },
  );

  console.info("The reviewed Google identity link is present.");
}

async function main() {
  try {
    await linkLegacyGoogleAccount();
  } catch (error: unknown) {
    console.error(
      error instanceof Error
        ? `Google account linking refused: ${error.message}`
        : "Google account linking refused.",
    );
    process.exitCode = 1;
  } finally {
    await prisma.$disconnect();
  }
}

void main();
