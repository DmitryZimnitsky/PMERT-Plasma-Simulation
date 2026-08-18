# PMERT-Plasma-Simulation
# Passive Metasurface Electromagnetic-Resonance Tokamak (PMER-T)

Mathematical 1D-model and physics framework for a steady-state fusion reactor utilizing passive metasurface recycling of electron cyclotron radiation for ICR-heating and ECC-Current Drive.

## ⚛️ Physical Paradigm & Framework

The PMER-T concept completely bypasses heavy and inefficient external auxiliary heating systems (neutral beam injectors, external gyrotrons). Instead, it turns the plasma's worst enemy—**cyclotron radiation loss**—into its primary asset.

1. **Phase 1: Induction Spark (0–0.5 ms):** A central solenoid induces an initial 5 kV loop voltage, ionizing the deuterium fuel and creating a plasma seed.
2. **Phase 2: Recycled ICR-Heating (0.5 ms – 2.4 s):** At a magnetic field of $B = 1.25\text{ T}$, electrons emit a chaotic microwave spectrum at the Electron Cyclotron Frequency ($f_{ce} \approx 35\text{ GHz}$). Passive sub-wavelength patch nanorectennas ($\approx 2.12\text{ mm}$ cell size) on diamond windows capture this energy with 78% efficiency. Nonlinear high-frequency components divide the frequency down to $19\text{ MHz}$, which matches the **Ion Cyclotron Resonance (ICR)** of Deuterium ions. High-efficiency KV-loop antennas ($12-15\text{ cm}$ diameter) re-emit this energy, pumping it directly into the heavy ions, breaking the Spitzer thermal detachment.
3. **Phase 3: Steady-State ECCD Ignition (2.4 s – Infinite):** As the ion temperature breaches $13\text{ keV}$, secondary tritium ($T$) from the $D(d,p)t$ branch burns aggressively via the High-Yield DT cycle. To maintain magnetohydrodynamic stability, an automatic proportional controller restricts density when $\beta$ reaches $4.5\%$. The metasurface switches its phase matrix to asymmetric **Electron Cyclotron Current Drive (ECCD)**. The microwaves push electrons poloidally, maintaining a steady-state $136.7\text{ MA}$ plasma current. The primary induction coil drops its power grid consumption to $0\text{ MW}$.

---

## 📐 Mathematical Model & Equations

The system evolution is governed by coupled nonlinear partial differential equations for thermal and particle transport balance:

$$\frac{3}{2} n_e \frac{dT_e}{dt} = P_{ohmic} - P_{ei} - P_{brem} - P_{cyc\_loss\_e} - P_{loss\_e}$$

$$\frac{3}{2} (n_D + n_T) \frac{dT_i}{dt} = P_{ei} + P_{DD\_charged} + P_{DT\_charged} + P_{cyc\_recycled} - P_{loss\_i} - P_{fuel\_cooling}$$

### Where:
* **Spitzer Resistivity & Ohmic Drive:**
  $$\eta_{spitzer} = 1.03 \times 10^{-4} \times \frac{\ln \Lambda}{T_e^{1.5}}$$
  $$P_{ohmic} = \frac{I_p^2 \cdot \eta_{spitzer} \cdot \frac{2\pi R}{\pi a^2}}{V_{plasma}}$$
* **Passive Metasurface Recovery Yield:**
  $$P_{cyc\_base} = 6.21 \times 10^{-14} \cdot n_e \cdot B^2 \cdot \left(\frac{T_e}{1000}\right)$$
  $$P_{cyc\_recycled} = P_{cyc\_base} \cdot K_{window} \cdot \eta_{meta}$$
  *(For $B = 1.25\text{ T}$, $K_{window} = 0.45$, and $\eta_{meta} = 0.78$)*
* **High-Yield Tritium Burning Branch:**
  $$\frac{dn_T}{dt} = 0.5 n_D^2 \langle\sigma v\rangle_{DD} - n_D n_T \langle\sigma v\rangle_{DT} - \frac{n_T}{\tau_E}$$
  $$P_{DT\_charged} = n_D n_T \langle\sigma v\rangle_{DT} \cdot E_{\alpha} \quad (E_{\alpha} = 3.5\text{ MeV})$$

---

## 📊 Steady-State Balanced Simulation Log ($B = 1.25\text{ T}$)

| Time (s) | Plasma Current (kA) | Toroidal Beta (%) | Ion Temp (keV) | Grid Input Power (MW) | Total Thermal Output (MW) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **0.4** | 13279.6 | 4.43% | 2.009 | 136.23 | 0.22 |
| **0.8** | 26793.1 | 20.28% | 7.240 | 62.88 | 1.06 |
| **1.2** | 40514.6 | 26.03% | 9.033 | 88.36 | 2.56 |
| **1.6** | 54248.4 | 30.50% | 10.286 | 112.66 | 4.27 |
| **2.0** | 67990.2 | 34.12% | 11.180 | 136.22 | 5.92 |
| **2.4** | 81737.9 | **1.86%** | 11.828 | 159.25 | **168.41** *(Breakeven Point)* |
| **2.8** | 95490.1 | 1.99% | 12.300 | 181.93 | 195.76 |
| **3.2** | 109245.9 | 2.10% | 12.644 | 204.34 | 217.32 |
| **3.6** | 123004.7 | 2.20% | 12.893 | 226.56 | 233.61 |
| **4.0** | 136765.9 | **2.29%** | **13.068** | **15.00** *(ECCD Engaged)* | **245.30** *(Steady State)* |

### Final Engineered Efficiency Metrics:
* **Active Solenoid Power (Post 2.4s):** $0\text{ MW}$ (ECCD self-sustained).
* **Grid Cryo & Magnetic Tracking Basal Cost:** $\approx 15\text{ MW}$.
* **Total Harvestable Thermal Power (Fusion + Bremsstrahlung):** $245.30\text{ MW}$.
* **Net System Gain ($Q_{engineered}$):** 
  $$Q = \frac{245.30\text{ MW}}{15\text{ MW}} = \mathbf{16.35}$$

---

## 🛠️ Code Architecture & Verification
The underlying simulation is built using a C# multicore data-parallel engine optimized for highly thread-dense CPUs (e.g., AMD Ryzen / Intel Core i5/i7/i9). It segments transient timelines into independent parallel execution chunks via `Parallel.For` loop with adaptive nanosecond time-stepping down to $5\times 10^{-9}\text{ s}$ during early high-voltage breakdowns.

### Compilation
Open the project in Visual Studio 2022, target `.NET 6.0` or higher, paste the codebase from `Program.cs` and run in **Release** configuration for maximum performance.
 (https://zenodo.org/records/21998920)
