import { useContext } from "react";

// Mengimpor AuthContext yang sudah dibuat.
import { AuthContext } from "../Context/authContext";

// Custom hook untuk membaca authentication state.
export function useAuth() {
    // Mengambil nilai dari Provider terdekat.
    const context = useContext(AuthContext);

    // Memastikan hook dipakai di dalam AuthProvider.
    if (context === null) {
        throw new Error("useAuth harus digunakan di dalam AuthProvider.");
    }

    // Mengembalikan state dan actions authentication.
    return context;
}