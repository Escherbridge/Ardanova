import { PrismaClient } from "@prisma/client";

const PLATFORM_ADMIN_EMAILS = ["manlytaco3@gmail.com", "atoozmc@gmail.com"];

async function seedAdmins() {
  const prisma = new PrismaClient();

  try {
    for (const email of PLATFORM_ADMIN_EMAILS) {
      const user = await prisma.user.findUnique({ where: { email } });

      if (!user) {
        await prisma.user.create({
          data: {
            email,
            name: email.split("@")[0]!,
            role: "ADMIN",
            userType: "VOLUNTEER",
            isVerified: false,
          },
        });
        console.log(`Created ADMIN user: ${email}`);
      } else if (user.role !== "ADMIN") {
        await prisma.user.update({
          where: { id: user.id },
          data: { role: "ADMIN" },
        });
        console.log(`Promoted to ADMIN: ${email}`);
      } else {
        console.log(`Already ADMIN: ${email}`);
      }
    }

    console.log("Done.");
  } finally {
    await prisma.$disconnect();
  }
}

seedAdmins().catch((e) => {
  console.error(e);
  process.exit(1);
});
