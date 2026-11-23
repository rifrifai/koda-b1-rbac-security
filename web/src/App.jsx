import LoginPage from "./Pages/LoginPage";
import { Routes, Route, Navigate } from "react-router";
import ProtectedRoute from "./Auth/ProtectedRoute";
import DashboardPage from "./Pages/DashboardPage";

function App() {

	return (
		<>
			<Routes>
				<Route path="/login" element={<LoginPage />} />
				<Route path="/dashboard" element={
					<ProtectedRoute>
						<DashboardPage />
					</ProtectedRoute>
					}
				/>
				<Route path="*" element={<Navigate to="/dashboard" replace />} />
			</Routes>
		</>
	)
}

export default App
