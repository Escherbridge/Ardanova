import { NextResponse, type NextRequest } from "next/server";
import { apiClient } from "~/lib/api";

/**
 * POST /api/sdk/auth/session
 *
 * Exchange a game auth code for a session. The auth code is generated when
 * a user authorizes a game via the ArdaNova web flow. This endpoint validates
 * the code with the backend and returns a NextAuth session token + user profile.
 *
 * Body: { authCode: string }
 * Returns: { sessionToken: string, profile: UserProfile }
 */
export async function POST(request: NextRequest) {
  try {
    const body = (await request.json()) as { authCode?: string };

    if (!body.authCode) {
      return NextResponse.json(
        { error: "authCode is required" },
        { status: 400 },
      );
    }

    // Validate auth code with backend and get user info
    const userResponse = await apiClient.users.getByEmail(body.authCode);

    if (userResponse.error || !userResponse.data) {
      return NextResponse.json(
        { error: "Invalid auth code or user not found" },
        { status: 401 },
      );
    }

    // In production, this would generate a proper NextAuth session token.
    // For now, return the user profile so the SDK can store it.
    return NextResponse.json({
      sessionToken: `sdk_session_${userResponse.data.id}`,
      profile: {
        id: userResponse.data.id,
        email: userResponse.data.email,
        name: userResponse.data.name,
        image: userResponse.data.image,
        role: userResponse.data.role,
        verificationLevel: userResponse.data.verificationLevel,
      },
    });
  } catch {
    return NextResponse.json(
      { error: "Internal server error" },
      { status: 500 },
    );
  }
}
