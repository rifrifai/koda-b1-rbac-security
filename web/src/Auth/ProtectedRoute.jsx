import { Navigate, useLocation } from "react-router";
import { useAuth } from "../Hooks/useAuth";

export default function ProtectedRoute({ children }) {
    const { status, isAuthenticated } = useAuth();

    const location = useLocation();

    if (status === "checking") {
        return (
            <main className="flex min-h-screen items-center justify-center bg-slate-950">
                <div className="text-center text-white">
                <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-slate-600 border-t-orange-500" />

                <p className="text-sm">Memeriksa sesi pengguna...</p>
                </div>
            </main>
        );
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace state={{ from: location }} />;
    }

    return children;
}