import Link from "next/link";
import { ArrowRight, Users, Vote, Briefcase, TrendingUp, Shield, Zap } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Input } from "~/components/ui/input";
import { auth } from "~/server/auth";
import { HydrateClient } from "~/trpc/server";

export default async function Home() {
  const session = await auth();

  return (
    <HydrateClient>
      <div className="min-h-screen gradient-mesh">
        {/* Navigation */}
        <nav className="border-b-2 border-border bg-background/80 backdrop-blur-sm sticky top-0 z-50">
          <div className="swiss-grid py-4">
            <div className="col-span-full flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-xl font-bold tracking-tight">
                  <span className="neon-subtle-cyan">ARDA</span>
                  <span className="text-foreground">NOVA</span>
                </span>
              </div>
              <div className="hidden md:flex items-center gap-8">
                <Link href="/projects" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  Projects
                </Link>
                <Link href="/marketplace" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  Opportunities
                </Link>
                <Link href="/guilds" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  Guilds
                </Link>
                <Link href="/governance" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  Governance
                </Link>
              </div>
              <div className="flex items-center gap-4">
                {session ? (
                  <div className="flex items-center gap-4">
                    <span className="text-sm text-muted-foreground hidden sm:inline">
                      {session.user?.name}
                    </span>
                    <Button variant="neon" size="sm" asChild>
                      <Link href="/dashboard">Dashboard</Link>
                    </Button>
                  </div>
                ) : (
                  <div className="flex items-center gap-3">
                    <Button variant="ghost" size="sm" asChild>
                      <Link href="/api/auth/signin">Sign In</Link>
                    </Button>
                    <Button variant="outline-neon" size="sm" asChild>
                      <Link href="/api/auth/signin">Get Started</Link>
                    </Button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </nav>

        {/* Hero Section */}
        <section className="swiss-grid py-24 md:py-32">
          <div className="col-span-full md:col-6 flex flex-col gap-6">
            <Badge variant="neon" size="lg" className="w-fit">
              Own What You Build
            </Badge>
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold tracking-tight">
              <span className="gradient-text">Community-Owned</span>
              <br />
              Project Management
            </h1>
            <p className="text-muted-foreground text-lg max-w-lg">
              Transform ideas into cooperative projects where every contributor becomes a co-owner.
              Democratic governance, transparent records, and fair profit-sharing for all.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 pt-4">
              <Button variant="neon" size="lg" asChild>
                <Link href="/api/auth/signin">
                  Start Building <ArrowRight className="ml-2 size-4" />
                </Link>
              </Button>
              <Button variant="outline" size="lg" asChild>
                <Link href="#features">Learn More</Link>
              </Button>
            </div>
          </div>
          <div className="col-span-full md:col-5 md:col-start-8 flex items-center justify-center mt-12 md:mt-0">
            <div className="w-full max-w-sm aspect-square border-2 border-neon-cyan/30 bg-card/50 flex items-center justify-center relative">
              <div className="absolute inset-0 bg-gradient-to-br from-neon-cyan/5 to-neon-pink/5" />
              <span className="neon-text-cyan text-7xl font-bold">AN</span>
            </div>
          </div>
        </section>

        {/* Features Section */}
        <section id="features" className="swiss-grid py-20 border-t-2 border-border">
          <div className="col-span-full mb-12">
            <h2 className="text-3xl md:text-4xl font-bold mb-4">Everything You Need</h2>
            <p className="text-muted-foreground text-lg max-w-2xl">
              From ideation to execution, ArdaNova provides all the tools and community support
              to build projects where workers share in the success.
            </p>
          </div>

          <div className="col-span-full grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-cyan flex items-center justify-center mb-4">
                  <Users className="size-6 text-neon-cyan" />
                </div>
                <CardTitle>Worker Ownership</CardTitle>
                <CardDescription>
                  Every contributor earns ownership shares. Your work builds equity, not just a paycheck.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-pink flex items-center justify-center mb-4">
                  <Vote className="size-6 text-neon-pink" />
                </div>
                <CardTitle>Democratic Governance</CardTitle>
                <CardDescription>
                  Member-led decision making. Every stakeholder has a voice in how projects are run.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-green flex items-center justify-center mb-4">
                  <Briefcase className="size-6 text-neon-green" />
                </div>
                <CardTitle>Skill Marketplace</CardTitle>
                <CardDescription>
                  Find meaningful work that matches your skills. Secure payments and reputation tracking.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-yellow flex items-center justify-center mb-4">
                  <TrendingUp className="size-6 text-neon-yellow" />
                </div>
                <CardTitle>Profit Sharing</CardTitle>
                <CardDescription>
                  When projects succeed, everyone shares in the rewards. Fair distribution built into every project.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-purple flex items-center justify-center mb-4">
                  <Shield className="size-6 text-neon-purple" />
                </div>
                <CardTitle>Protected Payments</CardTitle>
                <CardDescription>
                  Automated agreements ensure fair payment. No more chasing invoices or broken promises.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-primary flex items-center justify-center mb-4">
                  <Zap className="size-6 text-primary" />
                </div>
                <CardTitle>Recognition & Growth</CardTitle>
                <CardDescription>
                  Build your reputation as you contribute. Unlock new opportunities based on proven track record.
                </CardDescription>
              </CardHeader>
            </Card>
          </div>
        </section>

        {/* How It Works Section */}
        <section className="swiss-grid py-20 border-t-2 border-border">
          <div className="col-span-full mb-12">
            <h2 className="text-3xl md:text-4xl font-bold mb-4">How It Works</h2>
            <p className="text-muted-foreground text-lg max-w-2xl">
              A simple process to turn your ideas into community-owned projects.
            </p>
          </div>

          <div className="col-span-full grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-cyan bg-neon-cyan/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-cyan">01</span>
              </div>
              <h3 className="text-xl font-bold">Create Account</h3>
              <p className="text-muted-foreground">
                Sign up to access the full platform. Your identity stays secure while your contributions are tracked.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-pink bg-neon-pink/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-pink">02</span>
              </div>
              <h3 className="text-xl font-bold">Create or Join</h3>
              <p className="text-muted-foreground">
                Start your own cooperative project or join existing ones. Find work that matches your skills.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-green bg-neon-green/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-green">03</span>
              </div>
              <h3 className="text-xl font-bold">Collaborate</h3>
              <p className="text-muted-foreground">
                Work with the community, participate in decisions, and build something meaningful together.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-yellow bg-neon-yellow/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-yellow">04</span>
              </div>
              <h3 className="text-xl font-bold">Own & Grow</h3>
              <p className="text-muted-foreground">
                Earn ownership shares for your work. Build a portfolio of stakes in projects you believe in.
              </p>
            </div>
          </div>
        </section>

        {/* Stats Section */}
        <section className="swiss-grid py-16 border-t-2 border-border">
          <div className="col-span-full grid grid-cols-2 md:grid-cols-4 gap-8">
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text">$0</p>
              <p className="text-muted-foreground mt-2">Community Funded</p>
            </div>
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text-warm">0</p>
              <p className="text-muted-foreground mt-2">Active Projects</p>
            </div>
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text-cool">0</p>
              <p className="text-muted-foreground mt-2">Co-Owners</p>
            </div>
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold neon-subtle-cyan">0</p>
              <p className="text-muted-foreground mt-2">Tasks Completed</p>
            </div>
          </div>
        </section>

        {/* CTA Section */}
        <section className="swiss-grid py-20 border-t-2 border-border">
          <div className="col-span-full">
            <Card variant="neon" padding="lg">
              <div className="flex flex-col md:flex-row items-center justify-between gap-8">
                <div className="flex-1">
                  <h2 className="text-2xl md:text-3xl font-bold mb-4">
                    Ready to Own What You Build?
                  </h2>
                  <p className="text-muted-foreground max-w-lg">
                    Join a community where workers become owners. Start contributing to projects
                    that share success with everyone who helped build them.
                  </p>
                </div>
                <div className="flex flex-col sm:flex-row gap-4">
                  <Button variant="neon" size="lg" asChild>
                    <Link href="/api/auth/signin">
                      Get Started <ArrowRight className="ml-2 size-4" />
                    </Link>
                  </Button>
                  <Button variant="outline" size="lg" asChild>
                    <Link href="/projects">Browse Projects</Link>
                  </Button>
                </div>
              </div>
            </Card>
          </div>
        </section>

        {/* Newsletter */}
        <section className="swiss-grid py-16 border-t-2 border-border">
          <div className="col-span-full md:col-6 md:col-start-4 text-center">
            <h3 className="text-xl font-bold mb-4">Stay Updated</h3>
            <p className="text-muted-foreground mb-6">
              Subscribe to our newsletter for the latest updates and community news.
            </p>
            <div className="flex gap-3 max-w-md mx-auto">
              <Input
                variant="neon"
                placeholder="Enter your email"
                className="flex-1"
              />
              <Button variant="neon">Subscribe</Button>
            </div>
          </div>
        </section>

        {/* Footer */}
        <footer className="border-t-2 border-border">
          <div className="swiss-grid py-12">
            <div className="col-span-full md:col-4">
              <span className="text-xl font-bold tracking-tight">
                <span className="neon-subtle-cyan">ARDA</span>
                <span className="text-foreground">NOVA</span>
              </span>
              <p className="text-sm text-muted-foreground mt-4 max-w-xs">
                Community-owned project management. Where contributors become co-owners.
              </p>
            </div>
            <div className="col-span-full md:col-2 md:col-start-7 mt-8 md:mt-0">
              <h4 className="font-bold text-sm mb-4">Platform</h4>
              <ul className="flex flex-col gap-2 text-sm text-muted-foreground">
                <li><Link href="/projects" className="hover:text-foreground transition-colors">Projects</Link></li>
                <li><Link href="/marketplace" className="hover:text-foreground transition-colors">Opportunities</Link></li>
                <li><Link href="/guilds" className="hover:text-foreground transition-colors">Guilds</Link></li>
                <li><Link href="/governance" className="hover:text-foreground transition-colors">Governance</Link></li>
              </ul>
            </div>
            <div className="col-span-full md:col-2 mt-8 md:mt-0">
              <h4 className="font-bold text-sm mb-4">Resources</h4>
              <ul className="flex flex-col gap-2 text-sm text-muted-foreground">
                <li><Link href="/docs" className="hover:text-foreground transition-colors">Documentation</Link></li>
                <li><Link href="/api" className="hover:text-foreground transition-colors">API</Link></li>
                <li><Link href="/support" className="hover:text-foreground transition-colors">Support</Link></li>
              </ul>
            </div>
            <div className="col-span-full md:col-2 mt-8 md:mt-0">
              <h4 className="font-bold text-sm mb-4">Legal</h4>
              <ul className="flex flex-col gap-2 text-sm text-muted-foreground">
                <li><Link href="/privacy" className="hover:text-foreground transition-colors">Privacy</Link></li>
                <li><Link href="/terms" className="hover:text-foreground transition-colors">Terms</Link></li>
              </ul>
            </div>
          </div>
          <div className="swiss-grid py-4 border-t border-border">
            <p className="col-span-full text-xs text-muted-foreground text-center">
              &copy; {new Date().getFullYear()} ArdaNova. Building the cooperative economy.
            </p>
          </div>
        </footer>
      </div>
    </HydrateClient>
  );
}
