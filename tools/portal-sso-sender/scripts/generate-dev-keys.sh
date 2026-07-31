#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output_dir="${1:-$script_dir/../.dev-keys}"

mkdir -p "$output_dir"

openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out "$output_dir/portal-signing-private.pem"
openssl rsa -in "$output_dir/portal-signing-private.pem" -pubout -out "$output_dir/portal-signing-public.pem"

openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out "$output_dir/auth-decryption-private.pem"
openssl rsa -in "$output_dir/auth-decryption-private.pem" -pubout -out "$output_dir/auth-decryption-public.pem"

cat <<EOF
Generated development keys:
  Portal signing private : $output_dir/portal-signing-private.pem
  Portal signing public  : $output_dir/portal-signing-public.pem
  Auth decrypt private   : $output_dir/auth-decryption-private.pem
  Auth decrypt public    : $output_dir/auth-decryption-public.pem
EOF
