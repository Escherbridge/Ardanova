import Link from "next/link";
import { ArrowRight, Wallet, Vote, Store, Coins, Shield, Zap } from "lucide-react";

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
                  Marketplace
                </Link>
                <Link href="/dao" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  DAO
                </Link>
                <Link href="/exchange" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                  Exchange
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
                      <Link href="/api/auth/signin">Connect</Link>
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
              Web3 Native Platform
            </Badge>
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold tracking-tight">
              <span className="gradient-text">Decentralized</span>
              <br />
              Project Management
            </h1>
            <p className="text-muted-foreground text-lg max-w-lg">
              Transform innovative ideas into tangible solutions through decentralized collaboration,
              on-chain governance, and blockchain-powered tools built on Algorand.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 pt-4">
              <Button variant="neon" size="lg" asChild>
                <Link href="/api/auth/signin">
                  Launch App <ArrowRight className="ml-2 size-4" />
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
              From ideation to execution, ArdaNova provides all the tools and community support you need.
            </p>
          </div>

          <div className="col-span-full grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-cyan flex items-center justify-center mb-4">
                  <Wallet className="size-6 text-neon-cyan" />
                </div>
                <CardTitle>Wallet Integration</CardTitle>
                <CardDescription>
                  Connect your Algorand wallet for seamless on-chain interactions and asset management.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-pink flex items-center justify-center mb-4">
                  <Vote className="size-6 text-neon-pink" />
                </div>
                <CardTitle>DAO Governance</CardTitle>
                <CardDescription>
                  Participate in decentralized decision-making with on-chain proposals and voting.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-green flex items-center justify-center mb-4">
                  <Store className="size-6 text-neon-green" />
                </div>
                <CardTitle>Task Marketplace</CardTitle>
                <CardDescription>
                  Browse and claim bounties with escrow-protected payments and reputation tracking.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-yellow flex items-center justify-center mb-4">
                  <Coins className="size-6 text-neon-yellow" />
                </div>
                <CardTitle>Token Exchange</CardTitle>
                <CardDescription>
                  Swap tokens and provide liquidity in the decentralized exchange with low fees.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-neon-purple flex items-center justify-center mb-4">
                  <Shield className="size-6 text-neon-purple" />
                </div>
                <CardTitle>Escrow Protection</CardTitle>
                <CardDescription>
                  Secure funding through smart contract escrows that protect both parties.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card variant="interactive">
              <CardHeader>
                <div className="size-12 border-2 border-primary flex items-center justify-center mb-4">
                  <Zap className="size-6 text-primary" />
                </div>
                <CardTitle>Gamification</CardTitle>
                <CardDescription>
                  Earn XP, maintain streaks, and unlock achievements as you contribute to projects.
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
              A simple process to turn your ideas into successful decentralized projects.
            </p>
          </div>

          <div className="col-span-full grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-cyan bg-neon-cyan/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-cyan">01</span>
              </div>
              <h3 className="text-xl font-bold">Connect Wallet</h3>
              <p className="text-muted-foreground">
                Link your Algorand wallet to access the full platform features and manage your assets.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-pink bg-neon-pink/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-pink">02</span>
              </div>
              <h3 className="text-xl font-bold">Create or Join</h3>
              <p className="text-muted-foreground">
                Start a new project or join existing ones. Claim tasks from the marketplace.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-green bg-neon-green/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-green">03</span>
              </div>
              <h3 className="text-xl font-bold">Collaborate</h3>
              <p className="text-muted-foreground">
                Work with the community, participate in governance, and earn rewards for contributions.
              </p>
            </div>

            <div className="flex flex-col gap-4">
              <div className="size-16 border-2 border-neon-yellow bg-neon-yellow/10 flex items-center justify-center">
                <span className="text-2xl font-bold text-neon-yellow">04</span>
              </div>
              <h3 className="text-xl font-bold">Earn & Grow</h3>
              <p className="text-muted-foreground">
                Receive tokens for completed work, build reputation, and unlock new opportunities.
              </p>
            </div>
          </div>
        </section>

        {/* Stats Section */}
        <section className="swiss-grid py-16 border-t-2 border-border">
          <div className="col-span-full grid grid-cols-2 md:grid-cols-4 gap-8">
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text">$0</p>
              <p className="text-muted-foreground mt-2">Total Value Locked</p>
            </div>
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text-warm">0</p>
              <p className="text-muted-foreground mt-2">Active Projects</p>
            </div>
            <div className="text-center">
              <p className="text-4xl md:text-5xl font-bold gradient-text-cool">0</p>
              <p className="text-muted-foreground mt-2">Community Members</p>
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
                    Ready to Build the Future?
                  </h2>
                  <p className="text-muted-foreground max-w-lg">
                    Join the decentralized revolution. Connect your wallet and start contributing today.
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
              Subscribe to our newsletter for the latest updates and announcements.
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
                Decentralized project management for the next generation. Built on Algorand.
              </p>
            </div>
            <div className="col-span-full md:col-2 md:col-start-7 mt-8 md:mt-0">
              <h4 className="font-bold text-sm mb-4">Platform</h4>
              <ul className="flex flex-col gap-2 text-sm text-muted-foreground">
                <li><Link href="/projects" className="hover:text-foreground transition-colors">Projects</Link></li>
                <li><Link href="/marketplace" className="hover:text-foreground transition-colors">Marketplace</Link></li>
                <li><Link href="/dao" className="hover:text-foreground transition-colors">DAO</Link></li>
                <li><Link href="/exchange" className="hover:text-foreground transition-colors">Exchange</Link></li>
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
              &copy; {new Date().getFullYear()} ArdaNova. Built on Algorand.
            </p>
          </div>
        </footer>
      </div>
    </HydrateClient>
  );
}
