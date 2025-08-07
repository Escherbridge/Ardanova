import Link from "next/link";
import { ArrowRight, Lightbulb, Users, Rocket, TrendingUp, Shield, Zap } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { auth } from "~/server/auth";
import { HydrateClient } from "~/trpc/server";

export default async function Home() {
  const session = await auth();

  return (
    <HydrateClient>
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100">
        {/* Navigation */}
        <nav className="border-b bg-white/80 backdrop-blur-sm">
          <div className="container mx-auto flex h-16 items-center justify-between px-4">
            <div className="flex items-center space-x-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-600 to-purple-600">
                <span className="text-sm font-bold text-white">D</span>
              </div>
              <span className="text-xl font-bold text-slate-900">DoSo</span>
            </div>
            <div className="hidden md:flex items-center space-x-6">
              <Link href="#features" className="text-slate-600 hover:text-slate-900">Features</Link>
              <Link href="#how-it-works" className="text-slate-600 hover:text-slate-900">How it Works</Link>
              <Link href="#pricing" className="text-slate-600 hover:text-slate-900">Pricing</Link>
            </div>
            <div className="flex items-center space-x-4">
              {session ? (
                <div className="flex items-center space-x-4">
                  <span className="text-sm text-slate-600">Welcome, {session.user?.name}</span>
                  <Button asChild>
                    <Link href="/dashboard">Dashboard</Link>
                  </Button>
                </div>
              ) : (
                <div className="flex items-center space-x-2">
                  <Button variant="ghost" asChild>
                    <Link href="/api/auth/signin">Sign In</Link>
                  </Button>
                  <Button asChild>
                    <Link href="/api/auth/signin">Get Started</Link>
                  </Button>
                </div>
              )}
            </div>
          </div>
        </nav>

        {/* Hero Section */}
        <section className="container mx-auto px-4 py-20">
          <div className="text-center">
            <Badge variant="secondary" className="mb-4">
              🚀 The Social Network for Doing
            </Badge>
            <h1 className="mb-6 text-4xl font-bold tracking-tight text-slate-900 sm:text-6xl">
              Transform Ideas into
              <span className="bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent"> Reality</span>
            </h1>
            <p className="mx-auto mb-8 max-w-2xl text-lg text-slate-600">
              Join the community that turns innovative ideas into tangible solutions through collaboration, 
              support, and integrated business tools. From concept to successful business.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Button size="lg" asChild>
                <Link href="/api/auth/signin">
                  Start Your Project <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" asChild>
                <Link href="#how-it-works">Learn More</Link>
              </Button>
            </div>
          </div>
        </section>

        {/* Features Section */}
        <section id="features" className="container mx-auto px-4 py-20">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-4">Everything You Need to Succeed</h2>
            <p className="text-lg text-slate-600 max-w-2xl mx-auto">
              From ideation to execution, DoSo provides all the tools and community support you need.
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            <Card>
              <CardHeader>
                <Lightbulb className="h-10 w-10 text-blue-600 mb-2" />
                <CardTitle>Project Design</CardTitle>
                <CardDescription>
                  Define problems, outline solutions, and create compelling presentations with multimedia support.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card>
              <CardHeader>
                <Users className="h-10 w-10 text-green-600 mb-2" />
                <CardTitle>Community Support</CardTitle>
                <CardDescription>
                  Get votes, subscriptions, and volunteers from a targeted audience that believes in your vision.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card>
              <CardHeader>
                <Rocket className="h-10 w-10 text-purple-600 mb-2" />
                <CardTitle>Agency Collaboration</CardTitle>
                <CardDescription>
                  Connect with professional agencies for robust execution and scalable implementation.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card>
              <CardHeader>
                <TrendingUp className="h-10 w-10 text-orange-600 mb-2" />
                <CardTitle>Business Tools</CardTitle>
                <CardDescription>
                  Integrated invoicing, inventory management, marketing automation, and analytics dashboard.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card>
              <CardHeader>
                <Shield className="h-10 w-10 text-red-600 mb-2" />
                <CardTitle>Funding & Equity</CardTitle>
                <CardDescription>
                  Secure funding through community support and receive dividend-yielding shares from successful projects.
                </CardDescription>
              </CardHeader>
            </Card>

            <Card>
              <CardHeader>
                <Zap className="h-10 w-10 text-yellow-600 mb-2" />
                <CardTitle>Payment Integration</CardTitle>
                <CardDescription>
                  Accept payments via bank transfer, USSD, and cards with local payment gateway integration.
                </CardDescription>
              </CardHeader>
            </Card>
          </div>
        </section>

        {/* How It Works Section */}
        <section id="how-it-works" className="bg-white py-20">
          <div className="container mx-auto px-4">
            <div className="text-center mb-16">
              <h2 className="text-3xl font-bold text-slate-900 mb-4">How DoSo Works</h2>
              <p className="text-lg text-slate-600 max-w-2xl mx-auto">
                A simple 4-step process to turn your ideas into successful businesses.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
              <div className="text-center">
                <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <span className="text-2xl font-bold text-blue-600">1</span>
                </div>
                <h3 className="text-xl font-semibold mb-2">Design Your Project</h3>
                <p className="text-slate-600">Define the problem, outline your solution, and create a compelling presentation.</p>
              </div>

              <div className="text-center">
                <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <span className="text-2xl font-bold text-green-600">2</span>
                </div>
                <h3 className="text-xl font-semibold mb-2">Get Community Support</h3>
                <p className="text-slate-600">Present to targeted audiences and gather votes, subscriptions, and volunteers.</p>
              </div>

              <div className="text-center">
                <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <span className="text-2xl font-bold text-purple-600">3</span>
                </div>
                <h3 className="text-xl font-semibold mb-2">Secure Funding</h3>
                <p className="text-slate-600">Achieve majority support to unlock funding and professional agency partnerships.</p>
              </div>

              <div className="text-center">
                <div className="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <span className="text-2xl font-bold text-orange-600">4</span>
                </div>
                <h3 className="text-xl font-semibold mb-2">Build & Scale</h3>
                <p className="text-slate-600">Use integrated business tools to manage operations and grow your venture.</p>
              </div>
            </div>
          </div>
        </section>

        {/* CTA Section */}
        <section className="container mx-auto px-4 py-20">
          <div className="text-center bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl p-12 text-white">
            <h2 className="text-3xl font-bold mb-4">Ready to Turn Your Ideas into Reality?</h2>
            <p className="text-xl mb-8 opacity-90">
              Join thousands of innovators, supporters, and agencies building the future together.
            </p>
            <Button size="lg" variant="secondary" asChild>
              <Link href="/api/auth/signin">
                Get Started Today <ArrowRight className="ml-2 h-4 w-4" />
              </Link>
            </Button>
          </div>
        </section>

        {/* Footer */}
        <footer className="border-t bg-white">
          <div className="container mx-auto px-4 py-12">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
              <div>
                <div className="flex items-center space-x-2 mb-4">
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-600 to-purple-600">
                    <span className="text-sm font-bold text-white">D</span>
                  </div>
                  <span className="text-xl font-bold text-slate-900">DoSo</span>
                </div>
                <p className="text-slate-600">
                  The social network for doing. Transform ideas into reality through community collaboration.
                </p>
              </div>
              
              <div>
                <h3 className="font-semibold text-slate-900 mb-4">Platform</h3>
                <ul className="space-y-2 text-slate-600">
                  <li><Link href="#" className="hover:text-slate-900">Projects</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Agencies</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Business Tools</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Community</Link></li>
                </ul>
              </div>
              
              <div>
                <h3 className="font-semibold text-slate-900 mb-4">Resources</h3>
                <ul className="space-y-2 text-slate-600">
                  <li><Link href="#" className="hover:text-slate-900">Help Center</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Documentation</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">API</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Blog</Link></li>
                </ul>
              </div>
              
              <div>
                <h3 className="font-semibold text-slate-900 mb-4">Company</h3>
                <ul className="space-y-2 text-slate-600">
                  <li><Link href="#" className="hover:text-slate-900">About</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Careers</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Privacy</Link></li>
                  <li><Link href="#" className="hover:text-slate-900">Terms</Link></li>
                </ul>
              </div>
            </div>
            
            <div className="border-t mt-12 pt-8 text-center text-slate-600">
              <p>&copy; 2024 DoSo. All rights reserved.</p>
            </div>
          </div>
        </footer>
      </div>
    </HydrateClient>
  );
}
