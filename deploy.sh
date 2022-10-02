#!/bin/bash

set -Eeuo pipefail;

log() {
    local YELLOW='\033[1;33m';
    local NC='\033[0m';

    printf "${YELLOW} ${1} ${NC}\n";
}

docker_push() { # {1} = Local tag name, {2} = remote repository uri
    log "Uploading API docker image to repository ${2}";
    docker tag ${1}:latest ${2}:latest;
    docker push ${2}:latest;
}

log "This script requires docker and aws cli(2.8.0) to be present and configured in your path";
aws --version;
docker version;

ECR_STACK_NAME='meter-reading-ecr';
CLOUDFORMATION_STACK_NAME='meter-reading-cloudformation';

ACCOUNT_ID=$(aws sts get-caller-identity --query "Account" --output text);
USERNAME=$(aws iam get-user --query "User.UserName" --output text)
REGION=$(aws configure get region);

log "Using account ${ACCOUNT_ID}(${USERNAME}) in region ${REGION}";

log "To access Keyspaces, we need a service specific credential. The script will attempt to create one using the current user. Only two service specific credentials can be activated per user at a time."
log "If the script fails to create one, please remove at least one credential from AWS IAM console."

# The following array contains username for [0] index and password for [1] index
KEYSPACES_CREDENTIALS=($(aws iam create-service-specific-credential --user-name ${USERNAME} --service-name cassandra.amazonaws.com --output text --query 'ServiceSpecificCredential.[ServiceUserName,ServicePassword]'))
aws ssm put-parameter --name "KeyspaceUserName" --value "${KEYSPACES_CREDENTIALS[0]}" --type String --overwrite --no-cli-pager
aws ssm put-parameter --name "KeyspacePassword" --value "${KEYSPACES_CREDENTIALS[1]}" --type String --overwrite --no-cli-pager

log "Creating/Updating ECR Resources";
aws cloudformation deploy --template-file ./MeterReadingIaC/aws_ecr.yaml --stack-name ${ECR_STACK_NAME} --region ${REGION}

log "Building MeterReadingAPI";
docker build -f MeterReaderAPI.Dockerfile . -t meter-reading-api:latest;

log "Building MeterReadingProcessor";
docker build -f MeterReadingProcessor.Dockerfile . -t meter-reading-processor:latest;

ECR_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com";
log "Logging into ECR ${ECR_URI}";
aws ecr get-login-password --region ${REGION} | docker login --username AWS --password-stdin ${ECR_URI};

ECR_MR_API_URI=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${ECR_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingAPIRepositoryUri'].OutputValue" --output text);
docker_push "meter-reading-api" "${ECR_MR_API_URI}";

ECR_MR_PROCESSOR_URI=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${ECR_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingProcessRepositoryUri'].OutputValue" --output text);
docker_push "meter-reading-processor" "${ECR_MR_PROCESSOR_URI}";

log "Creating/Updating CloudFormation Resources - (This process may take some time)";
aws cloudformation deploy --template-file ./MeterReadingIaC/aws_cloudformation.yaml --stack-name ${CLOUDFORMATION_STACK_NAME} --region ${REGION} --capabilities CAPABILITY_NAMED_IAM;

FUNCTION_URL=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${CLOUDFORMATION_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingFunctionEndpoint'].OutputValue" --output text);
log "You can access function at ${FUNCTION_URL}?meterId=<value>&date=<value>";

BUCKET_URL=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${CLOUDFORMATION_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingBucketEndpoint'].OutputValue" --output text);
log "You can upload consumption data to S3 bucket at ${BUCKET_URL}";