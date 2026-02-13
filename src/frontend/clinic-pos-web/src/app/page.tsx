"use client";

import { FormEvent, useMemo, useState } from "react";

type Branch = { id: string; name: string };
type Patient = {
  id: string;
  tenantId: string;
  primaryBranchId: string | null;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  createdAt: string;
};

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080";

export default function Home() {
  const [tenantId, setTenantId] = useState("11111111-1111-1111-1111-111111111111");
  const [token, setToken] = useState("admin-token");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [selectedBranchId, setSelectedBranchId] = useState("");
  const [branches, setBranches] = useState<Branch[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(false);

  const headers = useMemo(
    () => ({
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      "X-Tenant-Id": tenantId,
    }),
    [tenantId, token]
  );

  const loadBranches = async () => {
    const response = await fetch(`${apiBaseUrl}/api/branches?tenantId=${tenantId}`, { headers });
    if (!response.ok) {
      throw new Error("Failed to load branches");
    }

    const payload: Branch[] = await response.json();
    setBranches(payload);
  };

  const loadPatients = async () => {
    setLoading(true);
    setStatus("");

    try {
      const query = new URLSearchParams({ tenantId });
      if (selectedBranchId) {
        query.set("branchId", selectedBranchId);
      }

      const response = await fetch(`${apiBaseUrl}/api/patients?${query.toString()}`, { headers });
      if (!response.ok) {
        throw new Error("Failed to load patients");
      }

      const payload: Patient[] = await response.json();
      setPatients(payload);
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Failed to load patients");
    } finally {
      setLoading(false);
    }
  };

  const createPatient = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus("");

    const response = await fetch(`${apiBaseUrl}/api/patients`, {
      method: "POST",
      headers,
      body: JSON.stringify({
        firstName,
        lastName,
        phoneNumber,
        tenantId,
        primaryBranchId: selectedBranchId || null,
      }),
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      setStatus(problem?.detail ?? "Create patient failed");
      return;
    }

    setFirstName("");
    setLastName("");
    setPhoneNumber("");
    setStatus("Patient created");
    await loadPatients();
  };

  return (
    <main className="container">
      <h1>Clinic POS</h1>

      <section className="card">
        <h2>Session</h2>
        <div className="grid">
          <label>
            Token
            <input value={token} onChange={(event) => setToken(event.target.value)} />
          </label>
          <label>
            Tenant ID
            <input value={tenantId} onChange={(event) => setTenantId(event.target.value)} />
          </label>
          <button type="button" onClick={loadBranches}>
            Load Branches
          </button>
        </div>
      </section>

      <section className="card">
        <h2>Create Patient</h2>
        <form onSubmit={createPatient} className="grid">
          <label>
            First Name
            <input value={firstName} onChange={(event) => setFirstName(event.target.value)} required />
          </label>
          <label>
            Last Name
            <input value={lastName} onChange={(event) => setLastName(event.target.value)} required />
          </label>
          <label>
            Phone Number
            <input value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} required />
          </label>
          <label>
            Branch
            <select value={selectedBranchId} onChange={(event) => setSelectedBranchId(event.target.value)}>
              <option value="">All</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </label>
          <button type="submit">Create Patient</button>
        </form>
      </section>

      <section className="card">
        <h2>Patients</h2>
        <button type="button" onClick={loadPatients} disabled={loading}>
          {loading ? "Loading..." : "Refresh"}
        </button>
        {status ? <p>{status}</p> : null}
        <ul>
          {patients.map((patient) => (
            <li key={patient.id}>
              {patient.firstName} {patient.lastName} - {patient.phoneNumber} - {new Date(patient.createdAt).toLocaleString()}
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
