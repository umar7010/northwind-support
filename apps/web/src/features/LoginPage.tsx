import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

export function LoginPage() {
    const navigate = useNavigate();
    const location = useLocation();

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError('');

        if (!email.trim() || !password) {
            setError('Please enter your email and password.');
            return;
        }

        setLoading(true);

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: email.trim(), password }),
            });

            if (!response.ok) {
                setError('Invalid email or password.');
                return;
            }

            const data = await response.json();

            localStorage.setItem('authToken', data.token);
            localStorage.setItem('agentName', data.agent.fullName);

            const destination =
                (location.state as { from?: string } | null)?.from ??
                new URLSearchParams(location.search).get('from');

            navigate(
                destination?.startsWith('/') && !destination.startsWith('//')
                    ? destination
                    : '/tickets',
                { replace: true }
            );
        } catch {
            setError('Unable to connect to the server.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="app__main">
            <h1>Agent Login</h1>

            <form onSubmit={handleSubmit}>
                <label htmlFor="email">Email</label>
                <input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                />

                <label htmlFor="password">Password</label>
                <input
                    id="password"
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                />

                {error && <p role="alert">{error}</p>}

                <button type="submit" disabled={loading}>
                    {loading ? 'Signing in...' : 'Sign in'}
                </button>
            </form>
        </main>
    );
}