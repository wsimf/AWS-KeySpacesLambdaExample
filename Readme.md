## Stack

The AWS stack is composed of a S3 bucket to store consumption data, two Lambda functions, one for processing files and the other for processing API requests and a Keyspace as the datastore.

![aws-diagram](https://user-images.githubusercontent.com/1941924/193439687-f5449852-5a40-4f5f-a4ba-bdc6e041d317.png)


## Setting up

To set up the infrasructure, you can execute the included `deploy.sh` script (in a `bash` environment). This script requires `docker` and `aws` CLI version 2.8.0 or higher. Please log into the CLI if you haven't already. It will

1. Create service specific credentials to access AWS Keyspaces and store them in SSM parameter store. This is required for Lambda function to access Keyspaces later.
   > Only two service specific credentials can be activated per user at a time. If the script fails to create one, please remove one of the credentials from the AWS IAM console or create another user.
2. Create a CloudFormation stack with 2 ECR resources to store docker images.
3. Build Docker images each for the API project and the processor project
4. Log into ECR through Docker CLI
5. Push the built Docker images to the ECR
6. Create a CloudFormation stack with the following resources:
   - An `AWS::Cassandra::Keyspace`
   - An `AWS::Cassandra::Table` in the keyspace to store data
   - An `AWS::S3::Bucket` to upload consumption data
   - An `AWS::IAM::Role` to allow a lambda function (processor function) to read S3 data in the created bucket, write data to the created Keyspace and write logs to CloudWatch
   - An `AWS::Lambda::Permission` to allow created S3 bucket to invoke the processor Lambda function
   - An `AWS::Lambda::Function` to process CSV files uploaded to the created S3 bucket
   - An `AWS::IAM::Role` to allow a Lambda function (API function) to read data in the created Keyspace and to write logs to CloudWatch
   - An `AWS::Lambda::Function` to handle API requests and respond with metering data
   - An `AWS::Lambda::Url` for the API function
   - An `AWS::Lambda::Permission` to invoke the API function (by any user - unauthenticated access)

## Cleanup

You can use `cleaup.sh` to clean up the resources created by the previous script. It will

1. Remove all files in the created S3 bucket
2. Remove all Docker images uploaded to the created ECR repositories
3. Remove the created CloudFormation stack

## Folder Structure

### MeterReadingAPI

Contains the .NET 6 project for responding to cosumption API requests. `MeterReaderAPI.Dockerfile` is used to package this project into a Docker image which eventually gets executed in the Lambda function.

### MeterReadingCore

Contains the common library shared with the API and Processor projcts

### MeterReadingProcessor

Contains the .NET 6 project for processing the uploaded consumption data. `MeterReadingProcessor.Dockerfile` is used to package this project into a Docker image which eventually gets executed in the Lambda function.

### MeterReadingIaC

Contains the Infrastructure as Code to deploy to AWS. The used IaC platform is AWS CloudFormation.

### Testing

Execute `dotnet test` to run the included unit tests

## Future Improvements

This is a bare bones implementation demonstrating the use case of consuming CSV data into a datastore and responding to an API request which uses the processed data.

AWS Keyspaces (based on Apache Cassandra) was selected as the datastore because of the requirement to process high amounts of data. Cassandra enables processing data at high speeds for applications that require low latency compared to the traditional RDBMS systems. However, it has higher management overhead due to denormalisation of data (as opposed to normalisation in RDBMS). This denormalisation enables high-speed read/write queries at the expense of data duplication. Since only a single query (sum of total consumption per meter per day) is used, this limitation is negligible at the current stage. However, it may become an issue if the project were to evolve.

AWS Lambda was used to host both .NET projects processing CSV data and API requests. Moving it to ECS (Fargate or self-managed) or EKS may be best if more requirements are needed. Both these projects are containarised with only minor changes required to convert those into full-blown ASP.NET or .NET background worker projects.

## CloudWatch Logs
#### Processor
<img width="1563" alt="CleanShot 2022-10-02 at 18 40 44" src="https://user-images.githubusercontent.com/1941924/193439846-a94ce72b-630d-4134-b30e-93d02e33c202.png">

#### API
<img width="1544" alt="CleanShot 2022-10-02 at 18 42 17" src="https://user-images.githubusercontent.com/1941924/193439865-98920feb-58fa-4f41-8d64-3cfba3b20d3c.png">
