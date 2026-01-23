# ArdaNova Smart Contracts

Algorand smart contracts written in PyTeal for the ArdaNova platform.

## Structure

```
contracts/
├── governance/     # DAO governance contracts (proposals, voting)
├── tokens/         # ASA token factories
├── ico/            # ICO lifecycle contracts
├── exchange/       # DEX/swap contracts
└── escrow/         # Task payment escrow contracts
```

## Requirements

- Python 3.10+
- PyTeal
- Algorand SDK

## Development

```bash
# Install dependencies
pip install pyteal py-algorand-sdk

# Compile contracts
python -m contracts.governance.Governor
```

## Deployment

Contracts are deployed to Algorand TestNet for development and MainNet for production.
