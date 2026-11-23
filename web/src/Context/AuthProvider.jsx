import { useCallback, useEffect, useMemo, useState } from "react";
import { AuthContext } from "./AuthContext";
import { ApiError, loginUser, refreshSession, logoutUser, getCurrentUser } from "../services/authApi";

const EMPTY_SESSION = { user: null, accessToken: null, expiresAtUtc: null };

export function AuthProvider({ children }) {
    const [auth, setAuth] = useState({ status: "checking", ...EMPTY_SESSION });

    const applySession = useCallback(({ user, accessToken, expiresAtUtc }) => {
        setAuth({ status: "authenticated", user, accessToken, expiresAtUtc });
    }, []);

    const clearSession = useCallback(() => {
        setAuth({ status: "anonymous", ...EMPTY_SESSION });
    }, []);

    useEffect(() => {
        let active = true;
        refreshSession()
        .then((session) => active && applySession(session))
        .catch(() => active && clearSession());
        return () => {
        active = false;
        };
    }, [applySession, clearSession]);

    const login = useCallback(
        async (credentials) => {
        const session = await loginUser(credentials);
        applySession(session);
        return session;
        },
        [applySession],
    );

    const refreshAccessToken = useCallback(async () => {
        try {
            const session = await refreshSession();
            applySession(session);
            return session;
        } catch (error) {
            if (error instanceof ApiError && [401, 403].includes(error.status))
            clearSession();
        throw error;
        }
    }, [applySession, clearSession]);

    const loadCurrentUser = useCallback(async () => {
        if (!auth.accessToken)
            throw new ApiError(401, "Access Token belum tersedia.");
        try {
            return await getCurrentUser(auth.accessToken);
        } catch (error) {
            if (!(error instanceof ApiError) || error.status !== 401) throw error;
                const { accessToken } = await refreshAccessToken();
            return getCurrentUser(accessToken);
        }
    }, [auth.accessToken, refreshAccessToken]);

    const logout = useCallback(async () => {
        await logoutUser();
        clearSession();
    }, [clearSession]);

    const contextValue = useMemo(
        () => ({...auth, isAuthenticated: auth.status === "authenticated" && Boolean(auth.accessToken),
            login, refreshAccessToken, loadCurrentUser, logout,
        }), 
        [auth, login, refreshAccessToken, loadCurrentUser, logout],
    );

    return (
        <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
    );
}