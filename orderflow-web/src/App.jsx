import { useEffect, useMemo, useState } from "react";

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000").replace(/\/$/, "");

async function apiFetch(path, { token, method, body } = {}) {
  const headers = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let res;
  try {
    res = await fetch(`${apiBaseUrl}${path}`, {
      method: method ?? "GET",
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new Error("Network error. Check API availability and CORS settings.");
  }

  const contentType = res.headers.get("content-type") ?? "";
  const isJson = contentType.includes("application/json");
  const payload = isJson ? await res.json().catch(() => null) : await res.text().catch(() => "");

  if (!res.ok) {
    const backendMessage =
      payload && typeof payload === "object"
        ? payload.detail || payload.title || payload.message || JSON.stringify(payload)
        : typeof payload === "string"
          ? payload
          : "";

    throw new Error(`${res.status} ${res.statusText}${backendMessage ? ` - ${backendMessage}` : ""}`);
  }

  return payload;
}

function usePersistedState(key, initialValue) {
  const [value, setValue] = useState(() => {
    const raw = localStorage.getItem(key);
    if (!raw) return initialValue;
    try {
      return JSON.parse(raw);
    } catch {
      return initialValue;
    }
  });

  useEffect(() => {
    localStorage.setItem(key, JSON.stringify(value));
  }, [key, value]);

  return [value, setValue];
}

function statusLabel(value) {
  const v = Number(value);
  if (v === 1) return "Pending";
  if (v === 2) return "Processing";
  if (v === 3) return "Completed";
  if (v === 4) return "Failed";
  return String(value ?? "");
}

function isoShort(value) {
  if (!value) return "";
  const s = String(value);
  return s.length > 19 ? s.replace("T", " ").slice(0, 19) : s.replace("T", " ");
}

export default function App() {
  const [token, setToken] = usePersistedState("orderflow.token", "");
  const [email, setEmail] = usePersistedState("orderflow.email", "gustavo@orderflow.dev");
  const [password, setPassword] = useState("StrongPass123!");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const [orders, setOrders] = useState([]);
  const [selectedOrder, setSelectedOrder] = useState(null);

  const [currency, setCurrency] = useState("BRL");

  const [sku1, setSku1] = useState("KB-001");
  const [name1, setName1] = useState("Gaming Keyboard");
  const [qty1, setQty1] = useState(1);
  const [price1, setPrice1] = useState(350);

  const [sku2, setSku2] = useState("MS-002");
  const [name2, setName2] = useState("Gaming Mouse");
  const [qty2, setQty2] = useState(2);
  const [price2, setPrice2] = useState(150);

  const isAuthed = useMemo(() => (token ?? "").trim().length > 0, [token]);

  async function login() {
    setBusy(true);
    setError("");
    try {
      const res = await apiFetch("/api/auth/login", {
        method: "POST",
        body: { email, password },
      });

      const nextToken = res?.accessToken ?? res?.token ?? res?.jwt ?? "";
      setToken(nextToken);

      if (!nextToken) {
        setError("Login succeeded but token was not found in the response.");
      } else {
        setSelectedOrder(null);
        await loadOrders(nextToken);
      }
    } catch (e) {
      setToken("");
      setError(e?.message ?? "Login failed");
    } finally {
      setBusy(false);
    }
  }

  function logout() {
    setToken("");
    setOrders([]);
    setSelectedOrder(null);
    setError("");
  }

  async function loadOrders(forcedToken) {
    const t = forcedToken ?? token;
    setBusy(true);
    setError("");
    try {
      const res = await apiFetch("/api/orders", { token: t });
      setOrders(Array.isArray(res) ? res : []);
    } catch (e) {
      setError(e?.message ?? "Failed loading orders");
    } finally {
      setBusy(false);
    }
  }

  async function loadOrderById(orderId) {
    setBusy(true);
    setError("");
    try {
      const res = await apiFetch(`/api/orders/${orderId}`, { token });
      setSelectedOrder(res);
    } catch (e) {
      setError(e?.message ?? "Failed loading order");
    } finally {
      setBusy(false);
    }
  }

  async function createOrder() {
    setBusy(true);
    setError("");
    try {
      const body = {
        currency,
        items: [
          { sku: sku1, name: name1, quantity: Number(qty1), unitPrice: Number(price1) },
          { sku: sku2, name: name2, quantity: Number(qty2), unitPrice: Number(price2) },
        ],
      };

      const res = await apiFetch("/api/orders", { token, method: "POST", body });
      await loadOrders();
      if (res?.orderId) await loadOrderById(res.orderId);
    } catch (e) {
      setError(e?.message ?? "Failed creating order");
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    if (isAuthed) loadOrders();
  }, [isAuthed]);

  return (
    <div className="container">
      <div className="shell">
        <div className="header">
          <div className="brand">
            <h1>OrderFlow Web</h1>
            <p>Minimal React client for login and orders.</p>
          </div>

          <div className="pill">
            <span className="kv">
              <span className="muted">API</span>
              <span className="mono">{apiBaseUrl}</span>
            </span>
            {isAuthed ? (
              <button className="btn" onClick={logout} disabled={busy}>
                Logout
              </button>
            ) : null}
          </div>
        </div>

        {error ? <div className="alert">{error}</div> : null}

        {!isAuthed ? (
          <div className="card" style={{ maxWidth: 520 }}>
            <div className="card-body">
              <div className="card-title">
                <h2>Login</h2>
              </div>

              <div className="form">
                <div className="field">
                  <div className="label">Email</div>
                  <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} />
                </div>

                <div className="field">
                  <div className="label">Password</div>
                  <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
                </div>

                <button className="btn btn-primary" onClick={login} disabled={busy}>
                  {busy ? "Working..." : "Login"}
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="grid-2">
            <div className="card">
              <div className="card-body">
                <div className="card-title">
                  <h2>Orders</h2>
                  <div className="actions">
                    <button className="btn" onClick={() => loadOrders()} disabled={busy}>
                      Refresh
                    </button>
                  </div>
                </div>

                <div className="list">
                  {orders.length === 0 ? (
                    <div className="muted">No orders yet.</div>
                  ) : (
                    orders.map((o) => {
                      const id = o.id ?? o.orderId;
                      return (
                        <button key={id} className="order-btn" onClick={() => loadOrderById(id)} disabled={busy}>
                          <div className="order-id mono">{id}</div>
                          <div className="order-meta">
                            <span className="kv">
                              <span className="muted">Currency</span>
                              <span>{o.currency ?? ""}</span>
                            </span>
                            <span className="kv">
                              <span className="muted">Status</span>
                              <span>{statusLabel(o.status)}</span>
                            </span>
                            <span className="kv">
                              <span className="muted">Created</span>
                              <span className="mono">{isoShort(o.createdAtUtc ?? o.createdAt ?? "")}</span>
                            </span>
                          </div>
                        </button>
                      );
                    })
                  )}
                </div>
              </div>
            </div>

            <div className="card">
              <div className="card-body">
                <div className="card-title">
                  <h2>Create Order</h2>
                </div>

                <div className="form">
                  <div className="field">
                    <div className="label">Currency</div>
                    <input className="input" value={currency} onChange={(e) => setCurrency(e.target.value)} />
                  </div>

                  <div className="card" style={{ boxShadow: "none" }}>
                    <div className="card-body" style={{ padding: 12 }}>
                      <div className="label" style={{ marginBottom: 10 }}>
                        Item 1
                      </div>
                      <div className="row-2">
                        <input className="input" value={sku1} onChange={(e) => setSku1(e.target.value)} placeholder="SKU" />
                        <input className="input" value={name1} onChange={(e) => setName1(e.target.value)} placeholder="Name" />
                        <input className="input" type="number" value={qty1} onChange={(e) => setQty1(e.target.value)} placeholder="Quantity" />
                        <input className="input" type="number" value={price1} onChange={(e) => setPrice1(e.target.value)} placeholder="Unit price" />
                      </div>
                    </div>
                  </div>

                  <div className="card" style={{ boxShadow: "none" }}>
                    <div className="card-body" style={{ padding: 12 }}>
                      <div className="label" style={{ marginBottom: 10 }}>
                        Item 2
                      </div>
                      <div className="row-2">
                        <input className="input" value={sku2} onChange={(e) => setSku2(e.target.value)} placeholder="SKU" />
                        <input className="input" value={name2} onChange={(e) => setName2(e.target.value)} placeholder="Name" />
                        <input className="input" type="number" value={qty2} onChange={(e) => setQty2(e.target.value)} placeholder="Quantity" />
                        <input className="input" type="number" value={price2} onChange={(e) => setPrice2(e.target.value)} placeholder="Unit price" />
                      </div>
                    </div>
                  </div>

                  <button className="btn btn-primary" onClick={createOrder} disabled={busy}>
                    {busy ? "Working..." : "Create"}
                  </button>

                  <div className="field" style={{ marginTop: 4 }}>
                    <div className="label">Selected Order</div>
                    <pre className="pre">{selectedOrder ? JSON.stringify(selectedOrder, null, 2) : "Select an order from the list."}</pre>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
