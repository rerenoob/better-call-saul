# Fix IAM Permissions for Marketing Bucket

## Problem
The GitHub Actions deployment is failing because the IAM user `github-actions-bettercallsaul` doesn't have permissions to access the marketing S3 bucket.

## Error Message
```
User: arn:aws:iam::946591677346:user/github-actions-bettercallsaul is not authorized to perform: s3:ListBucket on resource: "arn:aws:s3:::better-call-saul-marketing"
```

## Solution
You need to update the IAM policy for the GitHub Actions user to include permissions for the marketing bucket.

### Step 1: Find the IAM Policy
1. Go to AWS Console → IAM → Users
2. Find the user `github-actions-bettercallsaul`
3. Click on the user and go to the "Permissions" tab
4. Find the policy attached to this user (likely an inline policy)

### Step 2: Update the Policy
Add the following permissions to the policy:

```json
{
    "Effect": "Allow",
    "Action": [
        "s3:GetObject",
        "s3:PutObject", 
        "s3:DeleteObject",
        "s3:ListBucket",
        "s3:GetBucketLocation"
    ],
    "Resource": [
        "arn:aws:s3:::better-call-saul-marketing",
        "arn:aws:s3:::better-call-saul-marketing/*"
    ]
}
```

### Step 3: Alternative - Use AWS CLI
If you have AWS CLI access, you can update the policy with this command:

```bash
# First, get the current policy
aws iam get-user-policy --user-name github-actions-bettercallsaul --policy-name YourPolicyName

# Then update the policy JSON to include the marketing bucket permissions
# Upload the updated policy
aws iam put-user-policy --user-name github-actions-bettercallsaul --policy-name YourPolicyName --policy-document file://updated-policy.json
```

### Step 4: Test the Fix
After updating the policy, the deployment should work. You can test by:
1. Pushing a change to the marketing files
2. Or manually triggering the deployment workflow

## Complete Policy Example
Here's what the complete policy should look like:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ecr:*",
                "ecs:*",
                "s3:*",
                "cloudfront:*"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:PutObject",
                "s3:DeleteObject",
                "s3:ListBucket",
                "s3:GetBucketLocation"
            ],
            "Resource": [
                "arn:aws:s3:::better-call-saul-frontend-production",
                "arn:aws:s3:::better-call-saul-frontend-production/*",
                "arn:aws:s3:::better-call-saul-marketing",
                "arn:aws:s3:::better-call-saul-marketing/*",
                "arn:aws:s3:::better-call-saul-prod-production",
                "arn:aws:s3:::better-call-saul-prod-production/*"
            ]
        }
    ]
}
```

## Notes
- The GitHub Actions workflows have been optimized and should work once permissions are fixed
- The marketing bucket already exists: `better-call-saul-marketing`
- The frontend deployment is working correctly (it has the necessary permissions)
- Once permissions are updated, both marketing and frontend deployments will work automatically