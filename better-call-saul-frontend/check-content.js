// Check the actual content of the production site
import https from 'https';

const url = 'https://orange-island-0a659d210.1.azurestaticapps.net/';

console.log('Fetching content from:', url);

https
  .get(url, res => {
    let data = '';

    res.on('data', chunk => {
      data += chunk;
    });

    res.on('end', () => {
      console.log('Content length:', data.length);
      console.log('First 500 characters:');
      console.log(data.substring(0, 500));

      // Check for key elements
      if (data.includes('Login')) {
        console.log('✅ Login form detected');
      }
      if (data.includes('email')) {
        console.log('✅ Email field detected');
      }
      if (data.includes('password')) {
        console.log('✅ Password field detected');
      }
      if (data.includes('Better Call Saul')) {
        console.log('✅ App title detected');
      }
    });
  })
  .on('error', err => {
    console.log('Error:', err.message);
  });
