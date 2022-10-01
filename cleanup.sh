set -Eeuo pipefail;

log() {
    local YELLOW='\033[1;33m';
    local NC='\033[0m';

    printf "${YELLOW} ${1} ${NC}\n";
}

clean_repo() {
    log "Cleaning Images in ${1}";
    local IMAGES=($(aws ecr list-images --repository-name ${1} --query 'imageIds[].imageDigest' --output text));
    if [ ${#IMAGES[@]} -ne 0 ]; then
        local COMMAND="aws ecr batch-delete-image --repository-name ${1} --no-cli-pager --image-ids ";
        for image in "${IMAGES[@]}"; do
            COMMAND+="imageDigest=${image} ";
        done 

        eval "${COMMAND}";
    fi 
}

clean_bucket() {
    log "Cleaning bucket ${1}"
    aws s3 rm s3://${1} --recursive || true;
}

remove_stack() {
    log "Removing stack ${1}"
    aws cloudformation delete-stack --stack-name ${1} --no-cli-pager;
}

log "This script requires docker and aws cli(2.8.0) to be present and configured in your path";

ACCOUNT_ID=$(aws sts get-caller-identity --query "Account" --output text);

clean_bucket "${ACCOUNT_ID}-meter-readings-raw";

remove_stack "meter-reading-cloudformation";

clean_repo "meter-reading-processor";
clean_repo "meter-reading-api";

remove_stack "meter-reading-ecr";