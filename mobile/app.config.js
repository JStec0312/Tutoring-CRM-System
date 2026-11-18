module.exports = ({ config }) => {
  const iosClientId = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID;

  // Android uses autolinking and an explicit Web client ID, without Firebase files.
  if (!iosClientId) {
    return config;
  }

  const iosUrlScheme = "com.googleusercontent.apps." +
    iosClientId.replace(".apps.googleusercontent.com", "");

  return {
    ...config,
    plugins: [
      ...(config.plugins ?? []),
      ["react-native-nitro-google-signin", { iosUrlScheme }],
    ],
  };
};
