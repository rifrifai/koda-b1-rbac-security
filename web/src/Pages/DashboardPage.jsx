import { useEffect, useState } from "react";
import { useAuth } from "../Hooks/useAuth";

const getErr = (err, fallback) =>
    err instanceof Error ? err.message : fallback;

export default function DashboardPage() {
    const { user, expiresAtUtc, loadCurrentUser, refreshAccessToken, logout } =
        useAuth();
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState({
        fetch: false,
        refresh: false,
        logout: false,
    });
    const [errorMessage, setErrorMessage] = useState("");
    const [successMessage, setSuccessMessage] = useState("");

    useEffect(() => {
        let active = true;
        loadCurrentUser()
        .then((data) => active && setProfile(data))
        .catch(
            (err) =>
            active &&
            setErrorMessage(getErr(err, "Gagal mengakses Protected API.")),
        );
        return () => {
        active = false;
        };
    }, [loadCurrentUser]);

    const run = async (key, action, fallbackErr) => {
        if (loading[key]) return;
        setLoading((prev) => ({ ...prev, [key]: true }));
        setErrorMessage("");
        try {
        await action();
        } catch (err) {
        setErrorMessage(getErr(err, fallbackErr));
        } finally {
        setLoading((prev) => ({ ...prev, [key]: false }));
        }
    };

    const handleLoadProfile = () =>
        run(
        "fetch",
        async () => setProfile(await loadCurrentUser()),
        "Gagal mengambil data user.",
        );

    const handleRefresh = () =>
        run(
        "refresh",
        async () => {
            setSuccessMessage("");
            await refreshAccessToken();
            setSuccessMessage("Access Token berhasil diperbarui.");
        },
        "Gagal memperbarui Access Token.",
        );

    const handleLogout = () =>
        run("logout", logout, "Logout gagal. Silakan coba kembali.");

    return (
        <main className="min-h-screen bg-slate-100 p-6 text-slate-900">
        <div className="mx-auto max-w-2xl space-y-4">
            <header className="flex items-center justify-between border-b pb-4">
            <div>
                <h1 className="text-xl font-bold text-orange-600">
                KodaStore Dashboard
                </h1>
                <p className="text-sm text-slate-600">
                Welcome, {user?.displayName ?? "User"}
                </p>
            </div>
            <button
                type="button"
                onClick={handleLogout}
                disabled={loading.logout}
                className="rounded-md bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-50"
            >
                {loading.logout ? "Logging out..." : "Logout"}
            </button>
            </header>

            <section className="space-y-1 rounded-lg border bg-white p-4 text-sm">
            <h2 className="text-base font-bold">Authentication State</h2>
            <p>
                Status:{" "}
                <span className="font-semibold text-green-700">Authenticated</span>
            </p>
            <p>Email: {user?.email ?? "-"}</p>
            <p>
                Token Exp:{" "}
                {expiresAtUtc
                ? new Date(expiresAtUtc).toLocaleString("id-ID")
                : "-"}
            </p>
            </section>

            <section className="space-y-3 rounded-lg border bg-white p-4 text-sm">
            <h2 className="text-base font-bold">
                Protected API &amp; Refresh Token
            </h2>
            <div className="flex flex-wrap gap-2">
                <button
                type="button"
                onClick={handleLoadProfile}
                disabled={loading.fetch}
                className="rounded-md bg-orange-600 px-4 py-2 text-white disabled:opacity-50"
                >
                {loading.fetch ? "Loading..." : "Load Profile"}
                </button>
                <button
                type="button"
                onClick={handleRefresh}
                disabled={loading.refresh}
                className="rounded-md border border-orange-600 px-4 py-2 text-orange-700 disabled:opacity-50"
                >
                {loading.refresh ? "Refreshing..." : "Refresh Access Token"}
                </button>
            </div>

            {profile && (
                <div className="rounded bg-slate-50 p-3">
                <p>
                    <strong>User ID:</strong> {profile.id}
                </p>
                <p>
                    <strong>Email:</strong> {profile.email}
                </p>
                </div>
            )}
            </section>

            {successMessage && (
            <p
                role="status"
                className="rounded-md bg-green-50 p-3 text-sm text-green-800"
            >
                {successMessage}
            </p>
            )}
            {errorMessage && (
            <p
                role="alert"
                className="rounded-md bg-red-50 p-3 text-sm text-red-700"
            >
                {errorMessage}
            </p>
            )}
        </div>
        </main>
    );
}