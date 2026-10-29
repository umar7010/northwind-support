import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';

/** Application shell: the header, the navigation, and wherever the router puts the page. */
export function App() {
  const navigate = useNavigate();
  const agentName = localStorage.getItem('agentName');

  function handleLogout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('agentName');
    navigate('/login', { replace: true });
  }
  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>
        <div>
          <span>{agentName}</span>
          <button type="button" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}
