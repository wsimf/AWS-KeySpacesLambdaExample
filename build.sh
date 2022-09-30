#!/bin/bash

log() {
    local YELLOW='\033[1;33m';
    local NC='\033[0m';

    printf "${YELLOW} ${1} ${NC}\n";
}

log "This script requires docker and aws cli to be present and configured in your path";

ECR_STACK_NAME='meter-reading-ecr';
ACCOUNT_ID=$(aws sts get-caller-identity --query "Account" --output text);
REGION=$(aws configure get region);

log "Using account ${ACCOUNT_ID} in region ${REGION}";

log "Creating/Updating ECR Resources";
aws cloudformation deploy --template-file ./MeterReadingIaC/aws_ecr.yaml --stack-name meter-reading-ecr --region ${REGION}

log "Building MeterReadingAPI";
docker build -f MeterReaderAPI.Dockerfile . -t meter-reading-api:latest;

log "Building MeterReadingProcessor";
docker build -f MeterReadingProcessor.Dockerfile . -t meter-reading-processor:latest;

ECR_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com";
log "Logging into ECR ${ECR_URI}";
aws ecr get-login-password --region ${REGION} | docker login --username AWS --password-stdin ${ECR_URI};

ECR_MR_API_URI=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${ECR_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingAPIRepositoryUri'].OutputValue" --output text)
log "Uploading API docker image to repository ${ECR_MR_API_URI}";
docker tag meter-reading-api:latest ${ECR_MR_API_URI}:latest;
docker push ${ECR_MR_API_URI}:latest;

ECR_MR_PROCESSOR_URI=$(aws cloudformation describe-stacks --region ${REGION} --query "Stacks[?StackName=='${ECR_STACK_NAME}'][].Outputs[?OutputKey=='MeterReadingProcessRepositoryUri'].OutputValue" --output text)
log "Uploading Processor docker image to repository ${ECR_MR_PROCESSOR_URI}";
docker tag meter-reading-processor:latest ${ECR_MR_PROCESSOR_URI}:latest;
docker push ${ECR_MR_PROCESSOR_URI}:latest;

