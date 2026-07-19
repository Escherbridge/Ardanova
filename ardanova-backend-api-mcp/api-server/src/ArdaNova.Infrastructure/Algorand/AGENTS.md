# Algorand provider boundary

`Algorand:Provider` is an allow-listed runtime choice: `Simulated`, `Azoa`, or
`Legacy`. `Simulated` is an in-process, deterministic no-chain implementation.
`Legacy` owns the custodial mnemonic signer and is forbidden in Production
unless the operator sets the explicit break-glass flag and supplies signer
configuration. `Azoa` does not claim the address-based credential lifecycle it
cannot implement; capability checks keep those flows distinct.
