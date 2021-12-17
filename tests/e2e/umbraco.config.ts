const umbracoConfig = {
  environment: {
    baseUrl: process.env.URL || "http://localhost:9000"
  },
  user: {
    login: process.env.UMBRACO_USER_LOGIN || "user@example.com",
    password: process.env.UMBRACO_USER_PASSWORD || "Umbraco9Rocks!"
  },
  member: {
    login: process.env.UMBRACO_MEMBER_LOGIN || "member@example.com",
    password: process.env.UMBRACO_MEMBER_PASSWORD || "Umbraco9Rocks!"
  }
};

export { umbracoConfig };
