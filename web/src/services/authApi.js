const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export class ApiError extends Error {
	constructor(status, message) {
		super(message);
		this.name = "ApiError";
		this.status = status;
	}
}

async function apiRequest(path, { method = "GET", credentials = "omit", headers, body } = {},) 
{
	if (!API_BASE_URL)
    throw new ApiError(0, "VITE_API_BASE_URL belum dikonfigurasi.");

	const response = await fetch(`${API_BASE_URL}${path}`, {
		method,
		credentials,
		headers: { Accept: "application/json", ...headers },
		body,
	}).catch(() => {
		throw new ApiError(0, "Tidak dapat menghubungi API. Periksa server, jaringan, dan CORS.",);
	});

	const isJson = response.headers.get("content-type")?.includes("json");
	const data = isJson ? await response.json().catch(() => null) : null;

	if (!response.ok){
		const firstValidationErr = Object.values(data?.errors ?? {}).flat()[0];
    const message =
		response.status === 429
			? "Terlalu banyak request. Silakan coba kembali nanti."
			: response.status >= 500
			? "Server sedang mengalami gangguan."
			: (firstValidationErr ??
				data?.detail ??
				data?.title ??
				"Request gagal.");

		throw new ApiError(response.status, message);
	}

	return data;
}

const validateSession = (data) => {
if (
	!data?.accessToken ||
	!data?.expiresAtUtc ||
	!data?.user?.id ||
	!data?.user?.email
) {
	throw new Error("Format response authentication tidak sesuai.");
}
return data;
};

export const loginUser = async ({ email, password }) =>
validateSession(
	await apiRequest("/api/v1/auth/login", {
	method: "POST",
	credentials: "include",
	headers: { "Content-Type": "application/json" },
	body: JSON.stringify({ email: email.trim(), password }),
	}),
);

let refreshInFlight = null;
export const refreshSession = () =>
(refreshInFlight ??= apiRequest("/api/v1/auth/refresh", {
	method: "POST",
	credentials: "include",
})
	.then(validateSession)
	.finally(() => (refreshInFlight = null)));

export const getCurrentUser = async (accessToken) => {
if (!accessToken) throw new ApiError(401, "Access Token tidak tersedia.");
return apiRequest("/api/v1/auth/me", {
	headers: { Authorization: `Bearer ${accessToken}` },
});
};

export const logoutUser = async () => {
await apiRequest("/api/v1/auth/logout", {
	method: "POST",
	credentials: "include",
});
};