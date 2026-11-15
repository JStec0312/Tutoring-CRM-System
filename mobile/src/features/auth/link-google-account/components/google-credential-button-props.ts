export type GoogleCredentialButtonProps = {
  nonce: string;
  onCredential: (nonce: string, idToken: string) => void;
  onFailure: (nonce: string) => void;
  onCancel: (nonce: string) => void;
};
