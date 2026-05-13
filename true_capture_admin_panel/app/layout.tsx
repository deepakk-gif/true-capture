import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "True Capture — Admin",
  description: "Operational admin console for True Capture.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
